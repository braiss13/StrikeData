using HtmlAgilityPack;
using StrikeData.Data;
using StrikeData.Models;
using StrikeData.Models.Enums; // StatPerspective

namespace StrikeData.Services.TeamData
{
    public class WinTrendsImporter
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        private static readonly Dictionary<string, string> _trWTMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "All Games", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=all_games" },
            { "After Win", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=is_after_win" },
            { "After Loss", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=is_after_loss" },
            { "League Games", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=is_league" },
            { "Non League Games", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=non_league" },
            { "Division Games", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=is_division" },
            { "Non Division Games", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=non_division" },
            { "As Home", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=s_home" },
            { "As Away", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=is_away" },
            { "As Favorite", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=is_fav" },
            { "As Underdog", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=is_dog" },
            { "As Home Favorite", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=is_home_fav" },
            { "As Home Underdog", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=is_home_dog" },
            { "As Away Favorite", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=is_away_favs" },
            { "As Away Underdog", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=is_away_dog" },
            { "With No Rest", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=no_rest" },
            { "1 Day Off", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=one_day_off" },
            { "4+ Days Off", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=four_plus_days_off" },
            { "Second Game of Doubleheader", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=s_doubleheader" },
            { "With Rest Advantage", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=rest_advantage" },
            { "With Rest Disadvantage", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=rest_disadvantage" },
            { "Equal Rest", "https://www.teamrankings.com/mlb/trends/win_trends/?sc=equal_rest" },
        };

        public WinTrendsImporter(AppDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
        }

        public async Task ImportAllStatsAsyncWT()
        {
            foreach (var stat in _trWTMap)
                await ImportWinTrendsTeamStatsTR(stat.Key, stat.Value);
        }

        public async Task ImportWinTrendsTeamStatsTR(string statTypeName, string url)
        {
            var response = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'datatable')]");
            if (table == null) return;

            var rows = table.SelectNodes(".//tbody/tr");
            if (rows == null) return;

            // 1) StatType en categoría WinTrends
            var statType = _context.StatTypes
                .FirstOrDefault(s => s.Name == statTypeName && s.StatCategory.Name == "WinTrends");
            if (statType == null)
            {
                int categoryId = await GetWinTrendsCategoryIdAsync();
                statType = new StatType { Name = statTypeName, StatCategoryId = categoryId };
                _context.StatTypes.Add(statType);
                await _context.SaveChangesAsync();
            }

            // 2) Pre-cargar Teams en diccionario (CLAVE: nombre normalizado)
            var allTeams = _context.Teams.ToList();
            var teamsByNormName = allTeams
                .GroupBy(t => NormalizeName(t.Name))
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // 3) Pre-cargar TeamStats existentes para este StatType + Team (Perspective=Team)
            var existingStats = _context.TeamStats
                .Where(ts => ts.StatTypeId == statType.Id && ts.Perspective == StatPerspective.Team)
                .ToList()
                .ToDictionary(ts => ts.TeamId, ts => ts);

            foreach (var row in rows)
            {
                var cells = row.SelectNodes("td");
                if (cells == null || cells.Count < 4) continue; 

                var rawTeam = cells[0].InnerText;
                var normName = NormalizeName(rawTeam);
                if (string.IsNullOrWhiteSpace(normName)) continue;

                if (!teamsByNormName.TryGetValue(normName, out var team))
                {
                    team = new Team { Name = normName }; // guarda el nombre ya normalizado
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();

                    teamsByNormName[normName] = team; // añade a la cache para no reinsertar en este mismo run
                }

                // Buscar en cache de TeamStat ya existente
                if (!existingStats.TryGetValue(team.Id, out var stat))
                {
                    stat = new TeamStat
                    {
                        TeamId = team.Id,
                        StatTypeId = statType.Id,
                        Perspective = StatPerspective.Team
                    };
                    _context.TeamStats.Add(stat);
                    existingStats[team.Id] = stat;
                }

                stat.WinLossRecord = Utilities.CleanText(cells[1].InnerText);

                var winPctText = Utilities.CleanText(cells[2].InnerText).Replace("%", "").Trim();
                stat.WinPct = Utilities.Parse(winPctText);
            }

            await _context.SaveChangesAsync();
        }

        // Normaliza: limpia + mapea alias a nombre oficial
        private static string NormalizeName(string? raw)
        {
            var cleaned = Utilities.CleanText(raw ?? "");
            return TeamNameNormalizer.Normalize(cleaned);
        }


        private async Task<int> GetWinTrendsCategoryIdAsync()
        {
            var category = _context.StatCategories.FirstOrDefault(c => c.Name == "WinTrends");
            if (category == null)
            {
                category = new StatCategory { Name = "WinTrends" };
                _context.StatCategories.Add(category);
                await _context.SaveChangesAsync();
            }
            return category.Id;
        }

    }
}
