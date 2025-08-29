using HtmlAgilityPack;
using StrikeData.Data;
using StrikeData.Models;
using StrikeData.Models.Enums;
using StrikeData.Services.Normalization; // StatPerspective
using StrikeData.Services.StaticMaps;

namespace StrikeData.Services.TeamData.Importers
{
    public class CuriousFactsImporter
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        public CuriousFactsImporter(AppDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
        }

        // Método principal -> recorre el map y llama al método que hace scraping/guardado (mismo patrón que FieldingImporter)
        public async Task ImportAllStatsAsyncCF()
        {
            foreach (var stat in TeamRankingsMaps.CuriousFacts)
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

            var txt = Utilities.CleanText(cells[index].InnerText);
            if (string.IsNullOrWhiteSpace(txt)) return null;

            // Quitar símbolo de porcentaje (YRFI/NRFI) y normalizar
            txt = txt.Replace("%", "").Trim();
            txt = HtmlEntity.DeEntitize(txt);

            return Utilities.Parse(txt);
        }
    }
}
