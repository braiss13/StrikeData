using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;
using StrikeData.Services.Normalization;

namespace StrikeData.Services.MatchData
{
    /*
        Imports schedule and final linescore data from MLB StatsAPI.
        Iterates a date range (inclusive), upserts Match (one per gamePk),
        and upserts per-inning lines (MatchInning).
    */
    public class MatchImporter
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        public MatchImporter(AppDbContext context, HttpClient? httpClient = null)
        {
            _context = context;
            // Using a shared HttpClient if provided; otherwise a lightweight new instance.
            // If this importer runs frequently, consider injecting a single, long-lived HttpClient via DI.
            _httpClient = httpClient ?? new HttpClient();
        }

        /*
            Imports all games between <paramref name="startDate"/> and <paramref name="endDate"/> (inclusive).
            
            Implementation details:
            - Builds the MLB schedule endpoint per day and hydrates "linescore".
            - Upserts Match by gamePk and then the MatchInning collection.
            - Uses a per-run in-memory dictionary (teamsByName) to avoid N+1 team lookups.
            - Each daily batch is saved with a single SaveChangesAsync().
        */
        public async Task ImportMatchesAsync(DateTime startDate, DateTime endDate)
        {
            // Preload known teams by official Name for O(1) lookup; reduces DB chatter in the hot loop.
            var teamsByName = _context.Teams.ToDictionary(
                t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);

            // Iterate calendar days inclusive.
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                // MLB StatsAPI expects ISO date yyyy-MM-dd in the "schedule" endpoint.
                string dateParam = date.ToString("yyyy-MM-dd");
                string url =
                    $"https://statsapi.mlb.com/api/v1/schedule?sportId=1&date={dateParam}&gameType=R&hydrate=linescore";

                string json;
                try
                {
                    // Single GET per day; failures are non-fatal (skip the day and continue).
                    json = await _httpClient.GetStringAsync(url);
                }
                catch
                {
                    // Network error or transient problem—skip to the next day.
                    continue;
                }

                // parse using System.Text.Json for speed and allocations control.
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("dates", out var datesArr) ||
                    datesArr.ValueKind != JsonValueKind.Array)
                {
                    // No "dates" array means no games or unexpected shape; skip.
                    continue;
                }

                foreach (var dateEl in datesArr.EnumerateArray())
                {
                    if (!dateEl.TryGetProperty("games", out var gamesArr) ||
                        gamesArr.ValueKind != JsonValueKind.Array)
                    {
                        // No "games" array provided; nothing to do.
                        continue;
                    }

                    foreach (var gameEl in gamesArr.EnumerateArray())
                    {
                        long gamePk;
                        try
                        {
                            // gamePk is the stable MLB game identifier. key our Match on this.
                            gamePk = gameEl.GetProperty("gamePk").GetInt64();
                        }
                        catch
                        {
                            // Missing or invalid gamePk; skip this row safely.
                            continue;
                        }

                        // Try to load an existing Match including child innings for in-place update.
                        // Include() avoids lazy-load/N+1 and allows us to update the collection.
                        var match = _context.Matches
                            .Include(m => m.Innings)
                            .FirstOrDefault(m => m.GamePk == gamePk);

                        bool isNew = match == null;
                        if (isNew)
                        {
                            match = new Match { GamePk = gamePk };
                            _context.Matches.Add(match);
                        }

                        // Date / Time (stored as UTC) 
                        // MLB sends "gameDate" with timezone info; parse with Assume/Adjust to UTC fallback.
                        string gameDateStr =
                            gameEl.GetProperty("gameDate").GetString() ?? dateParam;
                        DateTime gameDate;
                        if (!DateTime.TryParse(
                                gameDateStr,
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                                out gameDate))
                        {
                            // Defensive fallback: use the requested day if parsing fails.
                            gameDate = date;
                        }
                        // Ensure save the value as UTC for consistent time queries.
                        match.Date = DateTime.SpecifyKind(gameDate, DateTimeKind.Utc);

                        // ----- Venue (optional) -----
                        if (gameEl.TryGetProperty("venue", out var venueEl) &&
                            venueEl.TryGetProperty("name", out var venueNameEl))
                        {
                            match.Venue = venueNameEl.GetString() ?? match.Venue;
                        }

                        // ----- Teams & Records -----
                        // Normalize team names to our canonical names (TeamNameNormalizer) and ensure both teams exist in DB using the in-memory cache.
                        var teamsEl = gameEl.GetProperty("teams");
                        var homeEl = teamsEl.GetProperty("home");
                        var awayEl = teamsEl.GetProperty("away");

                        string homeName =
                            homeEl.GetProperty("team").GetProperty("name").GetString() ?? "";
                        string awayName =
                            awayEl.GetProperty("team").GetProperty("name").GetString() ?? "";

                        string normalizedHomeName = TeamNameNormalizer.Normalize(homeName);
                        string normalizedAwayName = TeamNameNormalizer.Normalize(awayName);

                        // Upsert teams into the cache and DB if needed.
                        if (!teamsByName.TryGetValue(normalizedHomeName, out var homeTeam))
                        {
                            homeTeam = new Team { Name = normalizedHomeName };
                            _context.Teams.Add(homeTeam);
                            teamsByName[normalizedHomeName] = homeTeam;
                        }
                        if (!teamsByName.TryGetValue(normalizedAwayName, out var awayTeam))
                        {
                            awayTeam = new Team { Name = normalizedAwayName };
                            _context.Teams.Add(awayTeam);
                            teamsByName[normalizedAwayName] = awayTeam;
                        }
                        match.HomeTeamId = homeTeam.Id;
                        match.AwayTeamId = awayTeam.Id;

                        // League record for each side at the time of the game.
                        var homeRecord = homeEl.GetProperty("leagueRecord");
                        var awayRecord = awayEl.GetProperty("leagueRecord");
                        match.HomeWins  = TryGetInt(homeRecord, "wins");
                        match.HomeLosses= TryGetInt(homeRecord, "losses");
                        match.HomePct   = TryGetDecimal(homeRecord, "pct");
                        match.AwayWins  = TryGetInt(awayRecord, "wins");
                        match.AwayLosses= TryGetInt(awayRecord, "losses");
                        match.AwayPct   = TryGetDecimal(awayRecord, "pct");

                        // ----- Linescore (totals + per-inning) -----
                        // The "hydrate=linescore" ensures totals and an array per inning when available.
                        if (gameEl.TryGetProperty("linescore", out var linescore))
                        {
                            // Totals for each team (runs/hits/errors).
                            if (linescore.TryGetProperty("teams", out var lsTeams))
                            {
                                match.HomeRuns   = TryGetInt(lsTeams.GetProperty("home"), "runs");
                                match.HomeHits   = TryGetInt(lsTeams.GetProperty("home"), "hits");
                                match.HomeErrors = TryGetInt(lsTeams.GetProperty("home"), "errors");
                                match.AwayRuns   = TryGetInt(lsTeams.GetProperty("away"), "runs");
                                match.AwayHits   = TryGetInt(lsTeams.GetProperty("away"), "hits");
                                match.AwayErrors = TryGetInt(lsTeams.GetProperty("away"), "errors");

                                // Back-compat alias in our model (some views use HomeScore/AwayScore).
                                match.HomeScore  = match.HomeRuns;
                                match.AwayScore  = match.AwayRuns;
                            }

                            // Per-inning details. Not all games have full inning data (e.g. postponed/shortened).
                            if (linescore.TryGetProperty("innings", out var inningsArr) &&
                                inningsArr.ValueKind == JsonValueKind.Array)
                            {
                                // Build an index of existing in-memory innings to update in place.
                                var existingInnings = match.Innings.ToDictionary(
                                    mi => mi.InningNumber);

                                foreach (var innEl in inningsArr.EnumerateArray())
                                {
                                    // inning "num" can be missing in some edge cases; guard and skip.
                                    int? inningNumber = TryGetInt(innEl, "num");
                                    if (inningNumber == null)
                                        continue;

                                    int? homeRunsInn = null, homeHitsInn = null, homeErrorsInn = null;
                                    int? awayRunsInn = null, awayHitsInn = null, awayErrorsInn = null;

                                    if (innEl.TryGetProperty("home", out var homeInning))
                                    {
                                        homeRunsInn  = TryGetInt(homeInning, "runs");
                                        homeHitsInn  = TryGetInt(homeInning, "hits");
                                        homeErrorsInn= TryGetInt(homeInning, "errors");
                                    }
                                    if (innEl.TryGetProperty("away", out var awayInning))
                                    {
                                        awayRunsInn  = TryGetInt(awayInning, "runs");
                                        awayHitsInn  = TryGetInt(awayInning, "hits");
                                        awayErrorsInn= TryGetInt(awayInning, "errors");
                                    }

                                    // Upsert the inning row within the aggregate Match.
                                    if (!existingInnings.TryGetValue(inningNumber.Value, out var mi))
                                    {
                                        mi = new MatchInning
                                        {
                                            Match        = match,
                                            InningNumber = inningNumber.Value
                                        };
                                        match.Innings.Add(mi);
                                        existingInnings[inningNumber.Value] = mi;
                                    }

                                    mi.HomeRuns  = homeRunsInn;
                                    mi.HomeHits  = homeHitsInn;
                                    mi.HomeErrors= homeErrorsInn;
                                    mi.AwayRuns  = awayRunsInn;
                                    mi.AwayHits  = awayHitsInn;
                                    mi.AwayErrors= awayErrorsInn;
                                }
                            }
                        }
                    }
                }

                // Persist all upserts for this daily batch.  This reduces DB roundtrips significantly.
                await _context.SaveChangesAsync();
            }
        }

        /*
            Safely extracts an int from a JSON element property; returns null if missing or not numeric.
            Accepts numbers and numeric strings for robustness.
        */
        private static int? TryGetInt(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return null;
            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var n))
                return n;
            if (prop.ValueKind == JsonValueKind.String &&
                int.TryParse(prop.GetString(), out var n2))
                return n2;
            return null;
        }

        /*
            Safely extracts a decimal from a JSON element property; returns null if missing or unparsable.
            Handles both numeric and "%" suffixed strings (e.g. "0.542", "54.2%").
            Parsed with InvariantCulture to avoid locale issues.
        */
        private static decimal? TryGetDecimal(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return null;
            if (prop.ValueKind == JsonValueKind.Number &&
                prop.TryGetDecimal(out var d))
                return d;
            if (prop.ValueKind == JsonValueKind.String)
            {
                var s = prop.GetString();
                if (string.IsNullOrWhiteSpace(s))
                    return null;
                s = s.Replace("%", "").Trim();
                if (decimal.TryParse(
                        s,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var d2))
                {
                    return d2;
                }
            }
            return null;
        }

        /*
         Imports all 2025 regular season matches from Mar 27, 2025 up to
         yesterday in the Europe/Madrid timezone (CEST/CET aware).
         
         Rationale:
         - MLB games can end late at night local time; using a local "yesterday"
           avoids importing partially completed current-day games.
         - Convert local boundaries to UTC before querying the API.
        */
        public async Task ImportSeasonMatchesAsync()
        {
            var madrid  = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
            var today   = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, madrid).Date;
            var endDate = today.AddDays(-1);
            var start   = new DateTime(2025, 3, 27);
            await ImportMatchesAsync(start, endDate);
        }
    }
}
