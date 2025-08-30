// TeamScheduleImporter.cs
using StrikeData.Data;
using StrikeData.Models;
using StrikeData.Models.Scraping;
using StrikeData.Services.Normalization;
using StrikeData.Services.StaticMaps;
using StrikeData.Services.TeamData.Scrapers;

namespace StrikeData.Services.TeamData.Importers
{
    /// <summary>
    /// Imports team schedules and split summaries for an entire season
    /// using Baseball Almanac pages (via TeamScheduleScraper).
    /// Persists: TeamGames (per game), TeamMonthlySplits (per month), TeamOpponentSplits (per opponent).
    /// </summary>
    public class TeamScheduleImporter
    {
        private readonly AppDbContext _context;
        private readonly TeamScheduleScraper _scraper;

        public TeamScheduleImporter(AppDbContext context, TeamScheduleScraper scraper)
        {
            _context = context;
            _scraper = scraper;
        }

        /// <summary>
        /// Iterates over all known Baseball Almanac team codes and imports:
        /// - full schedule (per game data),
        /// - monthly splits, and
        /// - opponent splits
        /// for the given season.
        /// Only teams already present in the DB are processed.
        /// </summary>
        public async Task ImportAllTeamsScheduleAsync(int season)
        {
            foreach (var kvp in TeamCodeMap.CodeToName)
            {
                var code = kvp.Key;   // Baseball Almanac team code (e.g., "NYA")
                var name = kvp.Value; // Official name in our DB

                // Skip unknown teams to avoid creating teams implicitly here.
                var team = _context.Teams.FirstOrDefault(t => t.Name == name);
                if (team == null)
                    continue;

                TeamScheduleResultDto? result;
                try
                {
                    // Scrape one team/year page and return schedule + split DTOs.
                    result = await _scraper.GetTeamScheduleAndSplitsAsync(code, season);
                }
                catch
                {
                    // Some teams/years may not have a page; ignore failures silently here.
                    continue;
                }

                if (result == null) { continue; }

                // ======================
                // Games (per-game rows)
                // ======================
                foreach (var gameDto in result.Schedule)
                {
                    // The scraped "Opponent" text already includes a prefix ("vs " or "at ").
                    // We derive IsHome from that prefix and strip it from the opponent's name.
                    bool isHome = false;
                    string oppText = gameDto.Opponent.Trim();
                    string oppName = oppText;

                    if (oppText.StartsWith("vs ", StringComparison.OrdinalIgnoreCase))
                    {
                        isHome = true;
                        oppName = oppText[3..].Trim();
                    }
                    else if (oppText.StartsWith("at ", StringComparison.OrdinalIgnoreCase))
                    {
                        isHome = false;
                        oppName = oppText[3..].Trim();
                    }

                    // Normalize opponent name so it matches the Team.Name stored in our DB.
                    string normalized = TeamNameNormalizer.Normalize(oppName);
                    var opponentTeam = _context.Teams.FirstOrDefault(t => t.Name == normalized);
                    int? oppId = opponentTeam?.Id; // may be null if the opponent is not in our DB

                    // Idempotent upsert keyed by (TeamId, Season, GameNumber).
                    var existing = _context.TeamGames
                        .FirstOrDefault(x => x.TeamId == team.Id && x.Season == season && x.GameNumber == gameDto.GameNumber);
                    if (existing == null)
                    {
                        existing = new TeamGame();
                        _context.TeamGames.Add(existing);
                    }

                    existing.TeamId = team.Id;
                    existing.Season = season;
                    existing.GameNumber = gameDto.GameNumber;

                    // Store Date as UTC to align with PostgreSQL timestamp semantics.
                    existing.Date = DateTime.SpecifyKind(gameDto.Date, DateTimeKind.Utc);

                    existing.IsHome = isHome;
                    existing.OpponentTeamId = oppId;
                    existing.OpponentName = normalized; // keep normalized text even when OpponentTeamId is null
                    existing.Score = gameDto.Score;
                    existing.Decision = gameDto.Decision;
                    existing.Record = gameDto.Record;
                }

                // ======================
                // Monthly splits (per month)
                // ======================
                foreach (var ms in result.MonthlySplits)
                {
                    // Upsert by (Team, Season, Month) to keep monthly aggregates unique.
                    var existing = _context.TeamMonthlySplits
                        .FirstOrDefault(x => x.TeamId == team.Id && x.Season == season && x.Month == ms.Month);
                    if (existing == null)
                    {
                        existing = new TeamMonthlySplit();
                        _context.TeamMonthlySplits.Add(existing);
                    }

                    existing.TeamId = team.Id;
                    existing.Season = season;
                    existing.Month = ms.Month;
                    existing.Games = ms.Games;
                    existing.Wins = ms.Won;
                    existing.Losses = ms.Lost;
                    existing.WinPercentage = ms.WinPercentage;
                }

                // ======================
                // Opponent splits (per opponent)
                // ======================
                foreach (var ts in result.TeamSplits)
                {
                    // Normalize the opponent label to match our Team names.
                    string normalizedOpp = TeamNameNormalizer.Normalize(ts.Opponent);
                    var opponentTeam = _context.Teams.FirstOrDefault(t => t.Name == normalizedOpp);
                    int? oppId = opponentTeam?.Id;

                    // Upsert by (Team, Season, OpponentName).
                    var existing = _context.TeamOpponentSplits
                        .FirstOrDefault(x => x.TeamId == team.Id && x.Season == season && x.OpponentName == normalizedOpp);
                    if (existing == null)
                    {
                        existing = new TeamOpponentSplit();
                        _context.TeamOpponentSplits.Add(existing);
                    }

                    existing.TeamId = team.Id;
                    existing.Season = season;
                    existing.OpponentTeamId = oppId;
                    existing.OpponentName = normalizedOpp;
                    existing.Games = ts.Games;
                    existing.Wins = ts.Won;
                    existing.Losses = ts.Lost;
                    existing.WinPercentage = ts.WinPercentage;
                }

                // Persist
                await _context.SaveChangesAsync();
            }
        }
    }
}
