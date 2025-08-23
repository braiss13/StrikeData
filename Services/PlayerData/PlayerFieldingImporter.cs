using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;
using StrikeData.Services;
using StrikeData.Services.Common;

namespace StrikeData.Services.PlayerData
{
    /// <summary>
    /// Importa Fielding (primer bloque) para todos los equipos.
    /// - Empareja por NOMBRE normalizado + POSICIÓN PRINCIPAL guardada en BD.
    /// - Si el jugador aparece en varias posiciones en la tabla, SOLO se usa la fila cuya POS coincide con la posición principal.
    /// - Crea PlayerStatType si no existe (categoría "Fielding").
    /// </summary>
    public class PlayerFieldingImporter
    {
        private readonly AppDbContext _context;
        private readonly PlayerFieldingScraper _scraper;

        private static readonly string[] Metrics = new[]
        {
            "OUTS","TC","CH","PO","A","E","DP","PB","CASB","CACS","FLD%"
        };

        public PlayerFieldingImporter(AppDbContext context, PlayerFieldingScraper scraper)
        {
            _context = context;
            _scraper = scraper;
        }

        public async Task ImportAllTeamsPlayerFieldingAsync(int season)
        {
            Console.WriteLine($"[FieldingImporter] === INICIO IMPORT FIELDING (season={season}) ===");

            // 1) StatCategory
            var fieldingCat = await _context.StatCategories
                .FirstOrDefaultAsync(c => c.Name == "Fielding");
            if (fieldingCat == null)
            {
                fieldingCat = new StatCategory { Name = "Fielding" };
                _context.StatCategories.Add(fieldingCat);
                await _context.SaveChangesAsync();
                Console.WriteLine("[FieldingImporter] StatCategory 'Fielding' creada.");
            }
            else
            {
                Console.WriteLine("[FieldingImporter] StatCategory 'Fielding' encontrada.");
            }

            // 2) PlayerStatTypes
            var existingTypes = await _context.PlayerStatTypes
                .Where(pst => pst.StatCategoryId == fieldingCat.Id)
                .ToListAsync();
            var byName = existingTypes.ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);

            int created = 0;
            foreach (var m in Metrics)
            {
                if (!byName.ContainsKey(m))
                {
                    var t = new PlayerStatType
                    {
                        Name = m,
                        StatCategoryId = fieldingCat.Id
                    };
                    _context.PlayerStatTypes.Add(t);
                    byName[m] = t;
                    created++;
                }
            }
            if (created > 0) await _context.SaveChangesAsync();
            Console.WriteLine($"[FieldingImporter] PlayerStatTypes creados: {created}.");

            // 3) Jugadores en memoria: por TeamId -> (nombre normalizado -> Player)
            var allPlayers = await _context.Players
                .Include(p => p.Team)
                .AsNoTracking()
                .ToListAsync();

            Console.WriteLine($"[FieldingImporter] Players en memoria: {allPlayers.Count}.");

            var playersByTeam = allPlayers
                .GroupBy(p => p.TeamId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var dict = new Dictionary<string, Player>(StringComparer.OrdinalIgnoreCase);
                        foreach (var p in g)
                        {
                            var norm = Utilities.NormalizePlayerName(p.Name);
                            if (!dict.ContainsKey(norm))
                                dict[norm] = p;

                            // Soporte extra: "Apellido, Nombre"
                            var parts = p.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                var last = parts[^1];
                                var first = string.Join(' ', parts.Take(parts.Length - 1));
                                var alt = Utilities.NormalizePlayerName($"{last}, {first}");
                                if (!dict.ContainsKey(alt))
                                    dict[alt] = p;
                            }
                        }
                        return dict;
                    });

            // 4) Equipos
            foreach (var kv in TeamCodeMap.CodeToName)
            {
                var code = kv.Key;
                var teamName = kv.Value;

                var team = await _context.Teams.FirstOrDefaultAsync(t => t.Name == teamName);
                if (team == null)
                {
                    Console.WriteLine($"[FieldingImporter][INFO] Team '{teamName}' no existe en BD. Saltando.");
                    continue;
                }

                Console.WriteLine($"[FieldingImporter] Equipo: {team.Name} (Id={team.Id}) | code={code}");

                var rows = await _scraper.GetTeamFieldingRowsAsync(code, season);
                Console.WriteLine($"[FieldingImporter] Filas recibidas del scraper para {team.Name}: {rows.Count}");

                if (!playersByTeam.TryGetValue(team.Id, out var byPlayerName))
                {
                    Console.WriteLine($"[FieldingImporter][INFO] No hay jugadores en BD para TeamId={team.Id}. Saltando.");
                    continue;
                }

                // Agrupar por nombre normalizado y luego elegir la fila cuya POS coincida con la posición principal del jugador en BD
                var groupedByName = rows
                    .GroupBy(r => Utilities.NormalizePlayerName(r.Name))
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                int matchedPlayers = 0;
                int upserts = 0;

                // Para diagnóstico si no hay matches
                int localMatches = 0;
                List<string> sampleScraper = groupedByName.Keys.Take(3).ToList();
                List<string> sampleDb = byPlayerName.Keys.Take(3).ToList();

                foreach (var normName in groupedByName.Keys)
                {
                    if (!byPlayerName.TryGetValue(normName, out var player))
                        continue; // no existe el jugador por nombre

                    var playerMainPos = NormalizePos(player.Position);
                    if (string.IsNullOrWhiteSpace(playerMainPos))
                    {
                        // Si no tenemos posición en BD, como fallback tomamos la primera fila del scraper para ese nombre
                        var fallbackRow = groupedByName[normName].FirstOrDefault();
                        if (fallbackRow == null) continue;

                        matchedPlayers++;
                        localMatches++;

                        upserts += await UpsertAllMetricsAsync(player.Id, byName, fallbackRow.Values);
                        continue;
                    }

                    // Buscar una fila del scraper con POS compatible con la posición principal guardada
                    var candidate = groupedByName[normName]
                        .FirstOrDefault(r => IsPosMatch(playerMainPos, NormalizePos(r.Pos)));

                    if (candidate == null)
                    {
                        // (opcional) si no hay match exacto, no escribimos nada para este jugador
                        continue;
                    }

                    matchedPlayers++;
                    localMatches++;

                    upserts += await UpsertAllMetricsAsync(player.Id, byName, candidate.Values);
                }

                if (localMatches == 0)
                {
                    Console.WriteLine($"[FieldingImporter][DEBUG] Sin matches (name+pos) para {team.Name}. Ejemplos:");
                    Console.WriteLine($"   Scraper (norm): {string.Join(" | ", sampleScraper)}");
                    Console.WriteLine($"   BD (norm):      {string.Join(" | ", sampleDb)}");
                }

                await _context.SaveChangesAsync();
                Console.WriteLine($"[FieldingImporter] {team.Name}: jugadores emparejados={matchedPlayers}, upserts PlayerStats={upserts}.");
            }

            Console.WriteLine("[FieldingImporter] === FIN IMPORT FIELDING ===");
        }

        // ---------- helpers ----------

        private async Task<int> UpsertAllMetricsAsync(int playerId,
                                                      Dictionary<string, PlayerStatType> byName,
                                                      Dictionary<string, float?> values)
        {
            int count = 0;

            foreach (var kvp in values)
            {
                if (!byName.TryGetValue(kvp.Key, out var statType)) continue;

                var existing = await _context.PlayerStats
                    .FirstOrDefaultAsync(ps =>
                        ps.PlayerId == playerId &&
                        ps.PlayerStatTypeId == statType.Id);

                if (existing == null)
                {
                    _context.PlayerStats.Add(new PlayerStat
                    {
                        PlayerId = playerId,
                        PlayerStatTypeId = statType.Id,
                        Total = kvp.Value
                    });
                }
                else
                {
                    existing.Total = kvp.Value;
                }

                count++;
            }

            return count;
        }

        /// <summary>
        /// Normaliza la posición a un conjunto pequeño: P, C, 1B, 2B, 3B, SS, LF, CF, RF, OF, IF, DH, UT.
        /// </summary>
        private static string NormalizePos(string? pos)
        {
            if (string.IsNullOrWhiteSpace(pos)) return "";
            var p = pos.Trim().ToUpperInvariant();

            // Quitar decoraciones
            p = p.Replace(".", "").Replace("-", "/").Replace(" ", "");

            // Si viene compuesta ("RF/1B"), nos quedamos con cada token para matching posterior
            // Aquí devolvemos la cadena tal cual (ya upper/limpia).
            return p;
        }

        /// <summary>
        /// Determina si la POS del jugador en BD es compatible con la POS del scraper.
        /// Soporta equivalencias: OF = {LF, CF, RF}, IF = {1B, 2B, 3B, SS}, P = {P, SP, RP}.
        /// También soporta POS múltiples en el scraper: "RF/1B" etc.
        /// </summary>
        private static bool IsPosMatch(string playerMainPosNorm, string scrapedPosNorm)
        {
            if (string.IsNullOrWhiteSpace(scrapedPosNorm))
                return false;

            // Descomponer posibles múltiples posiciones en el scraper
            var scrapedTokens = scrapedPosNorm.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Función local para comprobar compatibilidad simple
            bool SingleMatch(string p, string s)
            {
                if (p == s) return true;

                // P ~ SP/RP
                if (p == "P" && (s == "P" || s == "SP" || s == "RP")) return true;

                // OF ~ LF/CF/RF
                if (p == "OF" && (s == "LF" || s == "CF" || s == "RF")) return true;

                // IF ~ 1B/2B/3B/SS
                if (p == "IF" && (s == "1B" || s == "2B" || s == "3B" || s == "SS")) return true;

                // UT (utility) lo consideramos compatible con cualquier posición de campo
                if (p == "UT") return true;

                // DH solo con DH
                // C con C, etc. (ya cubierto por p==s)
                return false;
            }

            foreach (var token in scrapedTokens)
            {
                if (SingleMatch(playerMainPosNorm, token))
                    return true;
            }

            return false;
        }
    }
}
