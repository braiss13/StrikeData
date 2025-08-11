using HtmlAgilityPack;
using StrikeData.Data;
using StrikeData.Models;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore;

namespace StrikeData.Services.TeamData
{
    public class PitchingImporter
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        public PitchingImporter(AppDbContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
        }

        // Devuelve el Id de la categoría Pitching, creando la categoría si aún no existe.
        private async Task<int> GetPitchingCategoryIdAsync()
        {
            var category = _context.StatCategories.FirstOrDefault(c => c.Name == "Pitching");
            if (category == null)
            {
                category = new StatCategory { Name = "Pitching" };
                _context.StatCategories.Add(category);
                await _context.SaveChangesAsync();
            }
            return category.Id;
        }

        // Llama a la API oficial de MLB para recuperar las estadísticas de pitching.
        private async Task<JArray> FetchTeamPitchingStatsMLB()
        {
            var url = "https://bdfed.stitch.mlbinfra.com/bdfed/stats/team?&env=prod&gameType=R&group=pitching&order=desc&sortStat=strikeouts&stats=season&season=2025&limit=30&offset=0";
            var response = await _httpClient.GetStringAsync(url);
            var json = JObject.Parse(response);
            var stats = (JArray)json["stats"];
            if (stats == null || !stats.Any())
            {
                Console.WriteLine("⚠️ No se encontraron estadísticas de pitching.");
                return new JArray();
            }
            return stats;
        }

        // Importa las estadísticas de pitching y las guarda en la base de datos.
        private async Task ImportTeamPitchingStatsMLB()
        {
            var statsArray = await FetchTeamPitchingStatsMLB();
            var pitchingCategoryId = await GetPitchingCategoryIdAsync();

            // Mapeo de nombres de campos de la API a las abreviaturas usadas en StatType.
            var statMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "era", "ERA" },                  // Earned Run Average
                { "shutouts", "SHO" },             // Shutouts
                { "completeGames", "CG" },         // Complete games
                { "saves", "SV" },                 // Saves
                { "saveOpportunities", "SVO" },    // Save opportunities
                { "inningsPitched", "IP" },        // Innings pitched
                { "hits", "H" },                   // Hits permitidos
                { "runs", "R" },                   // Carreras permitidas
                { "homeRuns", "HR" },              // Home runs permitidos
                { "wins", "W" },                   // Victorias
                { "strikeOuts", "SO" },            // Strikeouts (observa la mayúscula en “O”)
                { "whip", "WHIP" },                // Walks + Hits por entrada lanzada
                { "avg", "AVG" },                  // Promedio del rival
                { "battersFaced", "TBF" },         // Turnos enfrentados (batters faced)
                { "numberOfPitches", "NP" },       // Número de lanzamientos
                { "pitchesPerInning", "P/IP" },    // Lanzamientos por entrada
                { "gamesFinished", "GF" },         // Juegos finalizados
                { "holds", "HLD" },                // Holds
                { "intentionalWalks", "IBB" },     // Bases por bolas intencionales
                { "wildPitches", "WP" },           // Wild pitches
                { "strikeoutWalkRatio", "K/BB" }   // Relación strikeout/base por bolas
            };

            foreach (var statToken in statsArray)
            {
                // Cada elemento debe ser un JObject con los campos de estadísticas.
                if (statToken is not JObject teamStat)
                    continue;

                var teamNameRaw = teamStat["teamName"]?.ToString();
                if (string.IsNullOrWhiteSpace(teamNameRaw))
                {
                    Console.WriteLine("⚠️ Nombre de equipo no encontrado o vacío.");
                    continue;
                }
                var teamName = TeamNameNormalizer.Normalize(teamNameRaw);

                // Busca o crea el equipo.
                var team = _context.Teams.FirstOrDefault(t => t.Name == teamName);
                if (team == null)
                {
                    team = new Team { Name = teamName };
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();
                }

                // Recorre todas las estadísticas que quieres importar.
                foreach (var mapping in statMappings)
                {
                    var apiField = mapping.Key;
                    var shortName = mapping.Value;

                    // Comprueba que el campo existe en el JSON.
                    if (!teamStat.TryGetValue(apiField, out var token))
                        continue;

                    var rawValue = token?.ToString();
                    if (string.IsNullOrWhiteSpace(rawValue))
                        continue;

                    if (!float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float statValue))
                    {
                        Console.WriteLine($"⚠️ Valor inválido para {shortName} en {teamName}: '{rawValue}'");
                        continue;
                    }

                    // Busca o crea el tipo de estadística.
                    var statType = _context.StatTypes.FirstOrDefault(s => s.Name == shortName);
                    if (statType == null)
                    {
                        statType = new StatType { Name = shortName, StatCategoryId = pitchingCategoryId };
                        _context.StatTypes.Add(statType);
                        await _context.SaveChangesAsync();
                    }

                    // Busca o crea la estadística del equipo.
                    var stat = _context.TeamStats.FirstOrDefault(ts => ts.TeamId == team.Id && ts.StatTypeId == statType.Id);
                    if (stat == null)
                    {
                        stat = new TeamStat { TeamId = team.Id, StatTypeId = statType.Id };
                        _context.TeamStats.Add(stat);
                    }
                    stat.Total = statValue;
                }

                await _context.SaveChangesAsync();
            }
        }

    }
}