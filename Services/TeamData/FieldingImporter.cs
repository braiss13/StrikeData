using HtmlAgilityPack;
using StrikeData.Data;
using StrikeData.Models;

namespace StrikeData.Services.TeamData
{
    public class FieldingImporter
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        // Mapa de abreviaturas y URLs de TeamRankings para fielding.
        private static readonly Dictionary<string, string> _trFMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {

            { "DP", "https://www.teamrankings.com/mlb/stat/double-plays-per-game" },
            { "E", "https://www.teamrankings.com/mlb/stat/runs-per-game" }
        };

        public FieldingImporter(AppDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
        }

        // Método principal -> Contiene las llamadas a los dos métodos de obtención de datos (TeamRankings)
        public async Task ImportAllStatsAsyncF()
        {
            foreach (var stat in _trFMap)
            {
                await ImportHittingTeamStatsTR(stat.Key, stat.Value);
            }
        }

        // Método que realiza scrapping para obtener estadísticas de TeamRankings
        public async Task ImportHittingTeamStatsTR(string statTypeName, string url)
        {

            // Descarga la página html y crea un documento con todo el contenido
            var response = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            // Busca la etiqueta <table> dentro del doc
            var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'datatable')]");

            // Obtiene la primera fila de la tabla que sería el encabezado
            var header = table.SelectSingleNode(".//thead/tr");

            // Obtiene las filas pero saltando el primero que sería el encabezado
            var rows = table.SelectNodes(".//tr").Skip(1);

            // Se busca si el tipo de estadística ya está en la BD, sino se crea
            var statType = _context.StatTypes.FirstOrDefault(s => s.Name == statTypeName);
            if (statType == null)
            {

                // Obtener o crear la categoría Hitting y recuperar su Id
                int categoryId = await GetFieldingCategoryIdAsync();

                // Crear el nuevo tipo de estadística asociándolo a esa categoría
                statType = new StatType
                {
                    Name = statTypeName,
                    StatCategoryId = categoryId
                };

                _context.StatTypes.Add(statType);
                await _context.SaveChangesAsync();
            }

            foreach (var row in rows)
            {

                // Se obtiene cada celda de cada fila (sería el <td>)
                var cells = row.SelectNodes("td");

                // Como la tabla tiene Posición, Nombre Equipo y estadísticas (Current, Last 3...) si es menor a 8 se salta.
                if (cells == null || cells.Count < 8) continue;

                // Se obtiene el nombre del equipo y se normaliza para evitar abreviaciones o inconsistencias
                string rawTeamName = cells[1].InnerText.Trim();
                string teamName = TeamNameNormalizer.Normalize(rawTeamName);

                // Se valida si el equipo ya existe en la BD, caso contrario se crea y se guarda
                var team = _context.Teams.FirstOrDefault(t => t.Name == teamName);
                if (team == null)
                {
                    team = new Team { Name = teamName };
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();
                }

                // Se busca si está el TeamStat en la BD, sino se crea
                var stat = _context.TeamStats.FirstOrDefault(ts => ts.TeamId == team.Id && ts.StatTypeId == statType.Id);
                if (stat == null)
                {
                    stat = new TeamStat
                    {
                        TeamId = team.Id,
                        StatTypeId = statType.Id
                    };
                    _context.TeamStats.Add(stat);
                }

                // Se guarda cada valor en la propiedad correspondiente de TeamStat
                stat.CurrentSeason = Utilities.Parse(cells[2].InnerText);
                stat.Last3Games = Utilities.Parse(cells[3].InnerText);
                stat.LastGame = Utilities.Parse(cells[4].InnerText);
                stat.Home = Utilities.Parse(cells[5].InnerText);
                stat.Away = Utilities.Parse(cells[6].InnerText);
                stat.PrevSeason = Utilities.Parse(cells[7].InnerText);

            }

            // Guarda todo al final para evitar múltiples escrituras en la BD
            await _context.SaveChangesAsync();
        }

        private async Task<int> GetFieldingCategoryIdAsync()
        {
            var category = _context.StatCategories.FirstOrDefault(c => c.Name == "Fielding");
            if (category == null)
            {
                category = new StatCategory { Name = "Fielding" };
                _context.StatCategories.Add(category);
                await _context.SaveChangesAsync();
            }
            return category.Id;
        }

    }
}