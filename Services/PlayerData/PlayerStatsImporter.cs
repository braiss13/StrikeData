using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;

namespace StrikeData.Services.PlayerData
{
    public class PlayerStatsImporter
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        public PlayerStatsImporter(AppDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Función general para lanzar desde Index:
        /// 1) Importa todos los jugadores (roster 40-man)
        /// 2) Importa estadísticas (pitching + hitting) y las guarda en PlayerStats
        /// </summary>
        public async Task ImportAllPlayersAndStatsAsync(int season = 2025)
        {
            // 1) Roster (jugadores)
            var rosterImporter = new PlayerRosterImporter(_context);
            await rosterImporter.ImportAllPlayersAsync();

            // 2) Estadísticas (pitching + hitting)
            await ImportSeasonStatsAsync(season);
        }

        /// <summary>
        /// Descarga y guarda estadísticas de temporada para pitchers y hitters.
        /// - Pitching: group=pitching
        /// - Hitting:  group=hitting
        /// Crea PlayerStatType si no existe (usando StatCategory "Pitching"/"Hitting")
        /// </summary>
        public async Task ImportSeasonStatsAsync(int season = 2025)
        {
            // ==== PITCHING ====
            var pitchingUrl =
                $"https://bdfed.stitch.mlbinfra.com/bdfed/stats/player?stitch_env=prod&sportId=1&gameType=R&group=pitching&stats=season&season={season}&playerPool=ALL&sortStat=so&order=desc&limit=1000&offset=0";

            await ImportStatsGroupAsync(
                url: pitchingUrl,
                categoryName: "Pitching",
                // Mapa "nombre de la métrica" -> "campo JSON"
                statFieldMap: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    // Básicas
                    { "W", "wins" },
                    { "L", "losses" },
                    { "ERA", "era" },
                    { "G", "gamesPlayed" },
                    { "GS", "gamesStarted" },
                    { "CG", "completeGames" },
                    { "SHO", "shutouts" },
                    { "SV", "saves" },
                    { "SVO", "saveOpportunities" },
                    { "IP", "inningsPitched" },
                    { "R", "runs" },
                    { "H", "hits" },
                    { "ER", "earnedRuns" },
                    { "HR", "homeRuns" },
                    { "HB", "hitBatsmen" },
                    { "BB", "baseOnBalls" },
                    { "SO", "strikeOuts" },
                    { "WHIP", "whip" },
                    { "AVG", "avg" },
                    { "TBF", "battersFaced" },
                    { "NP", "numberOfPitches" },
                    { "P/IP", "pitchesPerInning" },
                    { "QS", "qualityStarts" },
                    { "GF", "gamesFinished" },
                    { "HLD", "holds" },
                    { "IBB", "intentionalWalks" },
                    { "WP", "wildPitches" },
                    { "BK", "balks" },
                    { "GDP", "groundIntoDoublePlay" },
                    { "GO/AO", "groundOutsToAirouts" },
                    { "SO/9", "strikeoutsPer9Inn" },
                    { "BB/9", "walksPer9Inn" },
                    { "H/9", "hitsPer9Inn" },
                    { "K/BB", "strikeoutWalkRatio" },
                    { "BABIP", "babip" },
                    { "SB", "stolenBases" },
                    { "CS", "caughtStealing" },
                    { "PK", "pickoffs" }
                },
                // Filtra por jugadores Pitcher (Position == "P"), por seguridad
                mustBePitcher: true
            );

            // ==== HITTING ====
            var hittingUrl =
                $"https://bdfed.stitch.mlbinfra.com/bdfed/stats/player?stitch_env=prod&sportId=1&gameType=R&group=hitting&stats=season&season={season}&playerPool=ALL&sortStat=runs&order=desc&limit=1000&offset=0";

            await ImportStatsGroupAsync(
                url: hittingUrl,
                categoryName: "Hitting",
                statFieldMap: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "G", "gamesPlayed" },
                    { "AB", "atBats" },
                    { "R", "runs" },
                    { "H", "hits" },
                    { "2B", "doubles" },
                    { "3B", "triples" },
                    { "HR", "homeRuns" },
                    { "RBI", "rbi" },
                    { "BB", "baseOnBalls" },
                    { "SO", "strikeOuts" },
                    { "SB", "stolenBases" },
                    { "CS", "caughtStealing" },
                    { "AVG", "avg" },
                    { "OBP", "obp" },
                    { "SLG", "slg" },
                    { "OPS", "ops" },
                    { "PA", "plateAppearances" },
                    { "HBP", "hitByPitch" },
                    { "SAC", "sacBunts" },
                    { "SF", "sacFlies" },
                    // GIDP a veces es "gidp" o "groundIntoDoublePlay"; aquí usamos gidp
                    { "GIDP", "gidp" },
                    { "GO/AO", "groundOutsToAirouts" },
                    { "XBH", "extraBaseHits" },
                    { "TB", "totalBases" },
                    { "IBB", "intentionalWalks" },
                    { "BABIP", "babip" },
                    { "ISO", "iso" },
                    { "AB/HR", "atBatsPerHomeRun" },
                    { "BB/K", "walksPerStrikeout" },
                    { "BB%", "walksPerPlateAppearance" },
                    { "SO%", "strikeoutsPerPlateAppearance" },
                    { "HR%", "homeRunsPerPlateAppearance" }
                },
                mustBePitcher: false
            );
        }

        /// <summary>
        /// Importa un "grupo" de estadísticas (pitching o hitting):
        /// - Descarga JSON masivo
        /// - Asegura StatCategory
        /// - Asegura PlayerStatType por cada clave del mapa
        /// - Asigna PlayerStat.Total para cada jugador y métrica
        /// </summary>
        private async Task ImportStatsGroupAsync(string url, string categoryName, Dictionary<string, string> statFieldMap, bool mustBePitcher)
        {
            // 1) Cargar/crear categoría
            var category = await _context.StatCategories
                .FirstOrDefaultAsync(c => c.Name == categoryName);
            if (category == null)
            {
                category = new StatCategory { Name = categoryName };
                _context.StatCategories.Add(category);
                await _context.SaveChangesAsync();
            }

            // 2) Asegurar PlayerStatType por cada clave del mapa
            //    Pre-cargar existentes en diccionario para no consultar 1x1
            var existingTypes = await _context.PlayerStatTypes
                .Where(pst => pst.StatCategoryId == category.Id)
                .ToListAsync();
            var typesByName = existingTypes.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            foreach (var metricName in statFieldMap.Keys)
            {
                if (!typesByName.ContainsKey(metricName))
                {
                    var pst = new PlayerStatType
                    {
                        Name = metricName,
                        StatCategoryId = category.Id
                    };
                    _context.PlayerStatTypes.Add(pst);
                    typesByName[metricName] = pst;
                }
            }
            await _context.SaveChangesAsync();

            // 3) Pre-cargar jugadores por MLB_Player_Id
            var playersWithId = await _context.Players
                .Where(p => p.MLB_Player_Id != null)
                .ToListAsync();
            var playersByMlbId = playersWithId.ToDictionary(p => p.MLB_Player_Id!.Value, p => p);

            // 4) Pre-cargar PlayerStats existentes para esta categoría
            //    (para evitar FirstOrDefault por cada métrica)
            var typeIds = typesByName.Values.Select(t => t.Id).ToList();

            var existingStats = await _context.PlayerStats
                .Where(ps => typeIds.Contains(ps.PlayerStatTypeId))
                .ToListAsync();

            // Diccionario por (PlayerId, PlayerStatTypeId)
            var statsByKey = existingStats.ToDictionary(
                ps => (ps.PlayerId, ps.PlayerStatTypeId),
                ps => ps);

            // 5) Descargar JSON de stats
            string json = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("stats", out var statsArr) || statsArr.ValueKind != JsonValueKind.Array)
                return;

            foreach (var item in statsArr.EnumerateArray())
            {
                // playerId
                if (!item.TryGetProperty("playerId", out var pidEl)) continue;
                if (!pidEl.TryGetInt32(out var playerIdFromApi))
                {
                    // a veces viene Int64
                    if (pidEl.ValueKind == JsonValueKind.Number && pidEl.TryGetInt64(out var pid64))
                        playerIdFromApi = unchecked((int)pid64);
                    else
                        continue;
                }

                // Buscar en nuestra tabla Players por MLB_Player_Id
                if (!playersByMlbId.TryGetValue(playerIdFromApi, out var player))
                    continue;

                // Filtrar por tipo de jugador si procede (según Position en Players)
                bool isPitcher = string.Equals(player.Position, "P", StringComparison.OrdinalIgnoreCase);
                if (mustBePitcher && !isPitcher) continue;
                if (!mustBePitcher && isPitcher) continue;

                // Para cada métrica del mapa, leemos el valor del JSON y lo guardamos en PlayerStats
                foreach (var kv in statFieldMap)
                {
                    var metricName = kv.Key;
                    var jsonField = kv.Value;

                    if (!item.TryGetProperty(jsonField, out var valEl))
                    {
                        // caso especial: IP puede venir como "inningsPitched"
                        if (metricName == "IP" && item.TryGetProperty("inningsPitched", out var alt))
                            valEl = alt;
                        else
                            continue;
                    }

                    float? value = null;
                    switch (valEl.ValueKind)
                    {
                        case JsonValueKind.Number:
                            if (valEl.TryGetDouble(out var d)) value = (float)d;
                            break;
                        case JsonValueKind.String:
                            var s = valEl.GetString();
                            if (string.IsNullOrWhiteSpace(s)) break;
                            s = s.Replace("%", "").Trim(); // por si viniera con símbolo (no suele)
                            if (float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f))
                                value = f;
                            break;
                        default:
                            break;
                    }

                    // Asegurar PlayerStatType
                    if (!typesByName.TryGetValue(metricName, out var pstType))
                        continue;

                    var key = (player.Id, pstType.Id);
                    if (!statsByKey.TryGetValue(key, out var ps))
                    {
                        ps = new PlayerStat
                        {
                            PlayerId = player.Id,
                            PlayerStatTypeId = pstType.Id,
                            Total = value
                        };
                        _context.PlayerStats.Add(ps);
                        statsByKey[key] = ps;
                    }
                    else
                    {
                        ps.Total = value;
                        _context.PlayerStats.Update(ps);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
