using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;
using StrikeData.Services.Common;

namespace StrikeData.Services.PlayerData
{
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

            var fieldingCat = await _context.StatCategories.FirstOrDefaultAsync(c => c.Name == "Fielding")
                              ?? new StatCategory { Name = "Fielding" };
            if (fieldingCat.Id == 0) { _context.StatCategories.Add(fieldingCat); await _context.SaveChangesAsync(); }

            var existingTypes = await _context.PlayerStatTypes
                .Where(pst => pst.StatCategoryId == fieldingCat.Id)
                .ToListAsync();

            var byName = existingTypes.ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);
            int created = 0;
            foreach (var m in Metrics)
            {
                if (!byName.ContainsKey(m))
                {
                    var t = new PlayerStatType { Name = m, StatCategoryId = fieldingCat.Id };
                    _context.PlayerStatTypes.Add(t);
                    byName[m] = t; created++;
                }
            }
            if (created > 0) await _context.SaveChangesAsync();
            Console.WriteLine($"[FieldingImporter] PlayerStatTypes creados: {created}.");

            // Cache jugadores por TeamId y (NombreNormalizado, Pos)
            var allPlayers = await _context.Players.Include(p => p.Team).AsNoTracking().ToListAsync();
            Console.WriteLine($"[FieldingImporter] Players en memoria: {allPlayers.Count}.");

            // clave: $"{Normalize(name)}|{posUpper}"
            static string Key(string name, string? pos)
                => $"{Utilities.NormalizePlayerName(name)}|{(pos ?? "").ToUpperInvariant()}";

            var playersByTeam = allPlayers
                .GroupBy(p => p.TeamId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var dict = new Dictionary<string, Player>(StringComparer.OrdinalIgnoreCase);
                        foreach (var p in g)
                        {
                            var posU = (p.Position ?? "").ToUpperInvariant();
                            var k1 = Key(p.Name, posU);
                            if (!dict.ContainsKey(k1)) dict[k1] = p;

                            // variante "Apellido, Nombre"
                            var parts = p.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                var last = parts[^1];
                                var first = string.Join(' ', parts.Take(parts.Length - 1));
                                var k2 = Key($"{last}, {first}", posU);
                                if (!dict.ContainsKey(k2)) dict[k2] = p;
                            }
                        }
                        return dict;
                    });

            foreach (var kv in TeamCodeMap.CodeToName)
            {
                var code = kv.Key;
                var teamName = kv.Value;

                var team = await _context.Teams.FirstOrDefaultAsync(t => t.Name == teamName);
                if (team == null) continue;

                Console.WriteLine($"[FieldingImporter] Equipo: {team.Name} (Id={team.Id}) | code={code}");

                var rows = await _scraper.GetTeamFieldingRowsAsync(code, season);
                Console.WriteLine($"[FieldingImporter] Filas recibidas del scraper para {team.Name}: {rows.Count}");

                if (!playersByTeam.TryGetValue(team.Id, out var byPlayerKey))
                {
                    Console.WriteLine($"[FieldingImporter][INFO] No hay jugadores en BD para TeamId={team.Id}. Saltando.");
                    continue;
                }

                // Anti-duplicados dentro de la misma ejecución (antes de SaveChanges)
                var seen = new HashSet<(int playerId, int typeId)>();

                int matchedPlayers = 0, upserts = 0;

                foreach (var row in rows)
                {
                    var k = Key(row.Name, row.Pos); // <-- nombre + POS del scraper
                    if (!byPlayerKey.TryGetValue(k, out var player))
                        continue; // si POS no coincide con la guardada, se ignora

                    matchedPlayers++;

                    foreach (var kvp in row.Values)
                    {
                        if (!byName.TryGetValue(kvp.Key, out var statType)) continue;

                        var keyPair = (player.Id, statType.Id);
                        if (seen.Contains(keyPair)) continue; // ya insertado/actualizado en este lote
                        seen.Add(keyPair);

                        var existing = await _context.PlayerStats
                            .FirstOrDefaultAsync(ps => ps.PlayerId == player.Id &&
                                                       ps.PlayerStatTypeId == statType.Id);

                        if (existing == null)
                        {
                            _context.PlayerStats.Add(new PlayerStat
                            {
                                PlayerId = player.Id,
                                PlayerStatTypeId = statType.Id,
                                Total = kvp.Value
                            });
                        }
                        else
                        {
                            existing.Total = kvp.Value;
                        }
                        upserts++;
                    }
                }

                await _context.SaveChangesAsync();
                Console.WriteLine($"[FieldingImporter] {team.Name}: jugadores emparejados={matchedPlayers}, upserts PlayerStats={upserts}.");
            }

            Console.WriteLine("[FieldingImporter] === FIN IMPORT FIELDING ===");
        }
    }
}
