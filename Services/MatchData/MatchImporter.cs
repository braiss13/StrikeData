using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;
using StrikeData.Services.Normalization;

namespace StrikeData.Services.MatchData
{
    /// <summary>
    /// Importador de calendario y resultados de partidos desde la API de MLB.  
    /// Descarga diariamente los partidos entre una fecha inicial y final,
    /// guarda/actualiza la entidad Match y crea/actualiza las líneas por
    /// entrada (MatchInning).
    /// </summary>
    public class MatchImporter
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        public MatchImporter(AppDbContext context, HttpClient? httpClient = null)
        {
            _context = context;
            _httpClient = httpClient ?? new HttpClient();
        }

        /// <summary>
        /// Importa todos los partidos entre <paramref name="startDate"/> y
        /// <paramref name="endDate"/> (inclusive).
        /// </summary>
        public async Task ImportMatchesAsync(DateTime startDate, DateTime endDate)
        {
            // Pre-cargar equipos existentes en diccionario por nombre para evitar múltiples consultas.
            var teamsByName = _context.Teams.ToDictionary(
                t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                string dateParam = date.ToString("yyyy-MM-dd");
                string url =
                    $"https://statsapi.mlb.com/api/v1/schedule?sportId=1&date={dateParam}&gameType=R&hydrate=linescore";

                string json;
                try
                {
                    json = await _httpClient.GetStringAsync(url);
                }
                catch
                {
                    // Si la petición falla (problemas de red, etc.), salta al día siguiente
                    continue;
                }

                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("dates", out var datesArr) ||
                    datesArr.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var dateEl in datesArr.EnumerateArray())
                {
                    if (!dateEl.TryGetProperty("games", out var gamesArr) ||
                        gamesArr.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var gameEl in gamesArr.EnumerateArray())
                    {
                        long gamePk;
                        try
                        {
                            gamePk = gameEl.GetProperty("gamePk").GetInt64();
                        }
                        catch
                        {
                            continue;
                        }

                        // Buscar el partido existente por GamePk (incluye las entradas).
                        var match = _context.Matches
                            .Include(m => m.Innings)
                            .FirstOrDefault(m => m.GamePk == gamePk);

                        bool isNew = match == null;
                        if (isNew)
                        {
                            match = new Match { GamePk = gamePk };
                            _context.Matches.Add(match);
                        }

                        // Fecha del partido.
                        string gameDateStr =
                            gameEl.GetProperty("gameDate").GetString() ?? dateParam;
                        DateTime gameDate;
                        if (!DateTime.TryParse(
                                gameDateStr,
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                                out gameDate))
                        {
                            gameDate = date;
                        }
                        match.Date = DateTime.SpecifyKind(gameDate, DateTimeKind.Utc);

                        // Estadio.
                        if (gameEl.TryGetProperty("venue", out var venueEl) &&
                            venueEl.TryGetProperty("name", out var venueNameEl))
                        {
                            match.Venue = venueNameEl.GetString() ?? match.Venue;
                        }

                        // Equipos y récords.
                        var teamsEl = gameEl.GetProperty("teams");
                        var homeEl = teamsEl.GetProperty("home");
                        var awayEl = teamsEl.GetProperty("away");

                        string homeName =
                            homeEl.GetProperty("team").GetProperty("name").GetString() ?? "";
                        string awayName =
                            awayEl.GetProperty("team").GetProperty("name").GetString() ?? "";

                        string normalizedHomeName = TeamNameNormalizer.Normalize(homeName);
                        string normalizedAwayName = TeamNameNormalizer.Normalize(awayName);

                        // Asegurar equipos en la base de datos.
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

                        // Récords de liga.
                        var homeRecord = homeEl.GetProperty("leagueRecord");
                        var awayRecord = awayEl.GetProperty("leagueRecord");
                        match.HomeWins  = TryGetInt(homeRecord, "wins");
                        match.HomeLosses= TryGetInt(homeRecord, "losses");
                        match.HomePct   = TryGetDecimal(homeRecord, "pct");
                        match.AwayWins  = TryGetInt(awayRecord, "wins");
                        match.AwayLosses= TryGetInt(awayRecord, "losses");
                        match.AwayPct   = TryGetDecimal(awayRecord, "pct");

                        // Líneas de anotación.
                        if (gameEl.TryGetProperty("linescore", out var linescore))
                        {
                            // Totales finales.
                            if (linescore.TryGetProperty("teams", out var lsTeams))
                            {
                                match.HomeRuns   = TryGetInt(lsTeams.GetProperty("home"), "runs");
                                match.HomeHits   = TryGetInt(lsTeams.GetProperty("home"), "hits");
                                match.HomeErrors = TryGetInt(lsTeams.GetProperty("home"), "errors");
                                match.AwayRuns   = TryGetInt(lsTeams.GetProperty("away"), "runs");
                                match.AwayHits   = TryGetInt(lsTeams.GetProperty("away"), "hits");
                                match.AwayErrors = TryGetInt(lsTeams.GetProperty("away"), "errors");
                                // Alias para compatibilidad: HomeScore/AwayScore.
                                match.HomeScore  = match.HomeRuns;
                                match.AwayScore  = match.AwayRuns;
                            }

                            // Detalle por entrada.
                            if (linescore.TryGetProperty("innings", out var inningsArr) &&
                                inningsArr.ValueKind == JsonValueKind.Array)
                            {
                                var existingInnings = match.Innings.ToDictionary(
                                    mi => mi.InningNumber);

                                foreach (var innEl in inningsArr.EnumerateArray())
                                {
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

                // Persistir cambios diarios.
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Intenta extraer un int de un elemento JSON. Devuelve null si no existe o no es numérico.
        /// </summary>
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

        /// <summary>
        /// Intenta extraer un decimal de un elemento JSON, eliminando el símbolo % si lo incluye.
        /// </summary>
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

        /// <summary>
        /// Importa todos los partidos de la temporada 2025 desde el 27 de marzo
        /// hasta el día anterior a la fecha actual en el huso horario de Europa/Madrid.
        /// </summary>
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
