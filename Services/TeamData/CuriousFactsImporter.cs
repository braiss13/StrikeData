using HtmlAgilityPack;
using StrikeData.Data;
using StrikeData.Models;
using StrikeData.Models.Enums; // StatPerspective

namespace StrikeData.Services.TeamData
{
    public class CuriousFactsImporter
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        // Mapa de abreviaturas y URLs de TeamRankings para "Curious Facts" (Other)
        private static readonly Dictionary<string, string> _trCMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "YRFI",     "https://www.teamrankings.com/mlb/stat/yes-run-first-inning-pct" },
            { "NRFI",     "https://www.teamrankings.com/mlb/stat/no-run-first-inning-pct" },
            { "OYRFI",    "https://www.teamrankings.com/mlb/stat/opponent-yes-run-first-inning-pct" },
            { "ONRFI",    "https://www.teamrankings.com/mlb/stat/opponent-no-run-first-inning-pct" },
            { "1IR/G",    "https://www.teamrankings.com/mlb/stat/1st-inning-runs-per-game" },
            { "2IR/G",    "https://www.teamrankings.com/mlb/stat/2nd-inning-runs-per-game" },
            { "3IR/G",    "https://www.teamrankings.com/mlb/stat/3rd-inning-runs-per-game" },
            { "4IR/G",    "https://www.teamrankings.com/mlb/stat/4th-inning-runs-per-game" },
            { "5IR/G",    "https://www.teamrankings.com/mlb/stat/5th-inning-runs-per-game" },
            { "6IR/G",    "https://www.teamrankings.com/mlb/stat/6th-inning-runs-per-game" },
            { "7IR/G",    "https://www.teamrankings.com/mlb/stat/7th-inning-runs-per-game" },
            { "8IR/G",    "https://www.teamrankings.com/mlb/stat/8th-inning-runs-per-game" },
            { "9IR/G",    "https://www.teamrankings.com/mlb/stat/9th-inning-runs-per-game" },
            { "XTRAIR/G", "https://www.teamrankings.com/mlb/stat/extra-inning-runs-per-game" },
            { "O1IR/G",   "https://www.teamrankings.com/mlb/stat/opponent-1st-inning-runs-per-game" },
            { "O2IR/G",   "https://www.teamrankings.com/mlb/stat/opponent-2nd-inning-runs-per-game" },
            { "O3IR/G",   "https://www.teamrankings.com/mlb/stat/opponent-3rd-inning-runs-per-game" },
            { "O4IR/G",   "https://www.teamrankings.com/mlb/stat/opponent-4th-inning-runs-per-game" },
            { "O5IR/G",   "https://www.teamrankings.com/mlb/stat/opponent-5th-inning-runs-per-game" },
            { "O6IR/G",   "https://www.teamrankings.com/mlb/stat/opponent-6th-inning-runs-per-game" },
            { "O7IR/G",   "https://www.teamrankings.com/mlb/stat/opponent-7th-inning-runs-per-game" },
            { "O8IR/G",   "https://www.teamrankings.com/mlb/stat/opponent-8th-inning-runs-per-game" },
            { "O9IR/G",   "https://www.teamrankings.com/mlb/stat/opponent-9th-inning-runs-per-game" },
            { "OXTRAIR/G","https://www.teamrankings.com/mlb/stat/opponent-extra-inning-runs-per-game" },
            { "F4IR/G",   "https://www.teamrankings.com/mlb/stat/first-4-innings-runs-per-game" },
            { "F5IR/G",   "https://www.teamrankings.com/mlb/stat/first-5-innings-runs-per-game" },
            { "F6IR/G",   "https://www.teamrankings.com/mlb/stat/first-6-innings-runs-per-game" },
            { "OF4IR/G",  "https://www.teamrankings.com/mlb/stat/opponent-first-4-innings-runs-per-game" },
            { "OF5IR/G",  "https://www.teamrankings.com/mlb/stat/opponent-first-5-innings-runs-per-game" },
            { "OF6IR/G",  "https://www.teamrankings.com/mlb/stat/opponent-first-6-innings-runs-per-game" },
            { "L2IR/G",   "https://www.teamrankings.com/mlb/stat/last-2-innings-runs-per-game" },
            { "L3IR/G",   "https://www.teamrankings.com/mlb/stat/last-3-innings-runs-per-game" },
            { "L4IR/G",   "https://www.teamrankings.com/mlb/stat/last-4-innings-runs-per-game" },
            { "OL2IR/G",  "https://www.teamrankings.com/mlb/stat/opponent-last-2-innings-runs-per-game" },
            { "OL3IR/G",  "https://www.teamrankings.com/mlb/stat/opponent-last-3-innings-runs-per-game" },
            { "OL4IR/G",  "https://www.teamrankings.com/mlb/stat/opponent-last-4-innings-runs-per-game" },
        };

        public CuriousFactsImporter(AppDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
        }

        // Método principal -> recorre el map y llama al método que hace scraping/guardado (mismo patrón que FieldingImporter)
        public async Task ImportAllStatsAsyncCF()
        {
            foreach (var stat in _trCMap)
            {
                await ImportCuriousTeamStatsTR(stat.Key, stat.Value);
            }
        }

        // Scraping TeamRankings + upsert en BD para una métrica concreta
        public async Task ImportCuriousTeamStatsTR(string statTypeKey, string url)
        {
            // Descarga la página html y crea un documento con todo el contenido
            var response = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            // Busca la tabla principal
            var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'datatable')]");
            if (table == null) return;

            // Filas: acotamos al tbody para evitar filas extrañas
            var rows = table.SelectNodes(".//tbody/tr");
            if (rows == null) return;

            // Determina perspectiva y abreviatura base del StatType (sin 'O' inicial)
            var perspective = statTypeKey.StartsWith("O", StringComparison.OrdinalIgnoreCase)
                ? StatPerspective.Opponent
                : StatPerspective.Team;

            var baseKey = perspective == StatPerspective.Opponent
                ? statTypeKey.Substring(1)
                : statTypeKey;

            // Asegura StatType dentro de la categoría CuriousFacts
            var statType = _context.StatTypes.FirstOrDefault(s => s.Name == baseKey);
            if (statType == null)
            {
                int categoryId = await GetCuriousFactsCategoryIdAsync();

                statType = new StatType
                {
                    Name = baseKey,
                    StatCategoryId = categoryId
                };

                _context.StatTypes.Add(statType);
                await _context.SaveChangesAsync();
            }

            foreach (var row in rows)
            {
                var cells = row.SelectNodes("td");
                // La tabla típica: Posición, Equipo, Current, Last 3, Last 1, Home, Away, Prev Season
                if (cells == null || cells.Count < 7) continue;

                // Normaliza nombre de equipo con tu utilidad
                string rawTeamName = cells[1].InnerText.Trim();
                string teamName = TeamNameNormalizer.Normalize(rawTeamName);

                // Asegura Team en BD
                var team = _context.Teams.FirstOrDefault(t => t.Name == teamName);
                if (team == null)
                {
                    team = new Team { Name = teamName };
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();
                }

                // Busca TeamStat por (Team, StatType, Perspective)
                var stat = _context.TeamStats.FirstOrDefault(ts =>
                    ts.TeamId == team.Id &&
                    ts.StatTypeId == statType.Id &&
                    ts.Perspective == perspective);

                if (stat == null)
                {
                    stat = new TeamStat
                    {
                        TeamId = team.Id,
                        StatTypeId = statType.Id,
                        Perspective = perspective
                    };
                    _context.TeamStats.Add(stat);
                }

                // Asignación de métricas (limpiando % donde corresponda)
                stat.CurrentSeason = ParseCell(cells, 2);
                stat.Last3Games    = ParseCell(cells, 3);
                stat.LastGame      = ParseCell(cells, 4);
                stat.Home          = ParseCell(cells, 5);
                stat.Away          = ParseCell(cells, 6);
                stat.PrevSeason    = cells.Count > 7 ? ParseCell(cells, 7) : null;
            }

            // Guarda todo al final
            await _context.SaveChangesAsync();
        }

        private async Task<int> GetCuriousFactsCategoryIdAsync()
        {
            var category = _context.StatCategories.FirstOrDefault(c => c.Name == "CuriousFacts");
            if (category == null)
            {
                category = new StatCategory { Name = "CuriousFacts" };
                _context.StatCategories.Add(category);
                await _context.SaveChangesAsync();
            }
            return category.Id;
        }

        // --- helpers ---

        private static float? ParseCell(IList<HtmlNode> cells, int index)
        {
            if (index >= cells.Count) return null;

            var txt = Utilites.CleanText(cells[index].InnerText);
            if (string.IsNullOrWhiteSpace(txt)) return null;

            // Quitar símbolo de porcentaje (YRFI/NRFI) y normalizar
            txt = txt.Replace("%", "").Trim();
            txt = HtmlEntity.DeEntitize(txt);

            return Utilites.Parse(txt);
        }
    }
}
