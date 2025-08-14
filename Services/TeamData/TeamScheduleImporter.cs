using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StrikeData.Data;
using StrikeData.Models;
using StrikeData.Models.Scraping;

namespace StrikeData.Services.TeamData
{
    public class TeamScheduleImporter
    {
        private readonly AppDbContext _context;
        private readonly TeamScheduleScraper _scraper;

        private static readonly Dictionary<string, string> TeamCodeToName = new()
        {
            ["TOR"] = "Toronto Blue Jays",
            ["BOS"] = "Boston Red Sox",
            ["NYA"] = "New York Yankees",
            ["TBR"] = "Tampa Bay Rays",
            ["BAL"] = "Baltimore Orioles",
            ["DET"] = "Detroit Tigers",
            ["CLG"] = "Cleveland Guardians",
            ["KCA"] = "Kansas City Royals",
            ["MIN"] = "Minnesota Twins",
            ["CHA"] = "Chicago White Sox",
            ["HOA"] = "Houston Astros",
            ["SEA"] = "Seattle Mariners",
            ["TEX"] = "Texas Rangers",
            ["ANG"] = "Los Angeles Angels",
            ["ATH"] = "Athletics",
            ["PHI"] = "Philadelphia Phillies",
            ["NYN"] = "New York Mets",
            ["MIA"] = "Miami Marlins",
            ["ATL"] = "Atlanta Braves",
            ["WS0"] = "Washington Nationals",
            ["ML4"] = "Milwaukee Brewers",
            ["CHN"] = "Chicago Cubs",
            ["CN5"] = "Cincinnati Reds",
            ["SLN"] = "St. Louis Cardinals",
            ["PIT"] = "Pittsburgh Pirates",
            ["SDN"] = "San Diego Padres",
            ["LAN"] = "Los Angeles Dodgers",
            ["ARI"] = "Arizona Diamondbacks",
            ["SFN"] = "San Francisco Giants",
            ["COL"] = "Colorado Rockies"
        };

        public TeamScheduleImporter(AppDbContext context, TeamScheduleScraper scraper)
        {
            _context = context;
            _scraper = scraper;
        }

        public async Task ImportAllTeamsScheduleAsync(int season = 2025)
        {
            foreach (var kvp in TeamCodeToName)
            {
                var code = kvp.Key;
                var name = kvp.Value;

                var team = _context.Teams.FirstOrDefault(t => t.Name == name);
                if (team == null)
                    continue; // Si el equipo no existe en la BD, lo ignoramos.

                TeamScheduleResultDto? result;
                try
                {
                    result = await _scraper.GetTeamScheduleAndSplitsAsync(code, season);
                }
                catch
                {
                    // La página puede no existir para ese equipo/año.
                    continue;
                }
                if (result == null)
                    continue;

                // Partidos del calendario
                foreach (var gameDto in result.Schedule)
                {
                    // Separar "vs " / "at "
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

                    // Normalizar el nombre del rival
                    string normalized = TeamNameNormalizer.Normalize(oppName);
                    var opponentTeam = _context.Teams.FirstOrDefault(t => t.Name == normalized);
                    int? oppId = opponentTeam?.Id;

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
                    existing.Date = gameDto.Date;
                    existing.IsHome = isHome;
                    existing.OpponentTeamId = oppId;
                    existing.OpponentName = normalized;
                    existing.Score = gameDto.Score;
                    existing.Decision = gameDto.Decision;
                    existing.Record = gameDto.Record;
                }

                // Splits mensuales
                foreach (var ms in result.MonthlySplits)
                {
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

                // Splits por rival
                foreach (var ts in result.TeamSplits)
                {
                    string normalizedOpp = TeamNameNormalizer.Normalize(ts.Opponent);
                    var opponentTeam = _context.Teams.FirstOrDefault(t => t.Name == normalizedOpp);
                    int? oppId = opponentTeam?.Id;

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

                await _context.SaveChangesAsync();
            }
        }
    }
}
