using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;
using StrikeData.Services.StaticMaps;

namespace StrikeData.Services.PlayerData
{
    
    // Imports the 40-man roster for all MLB teams (season 2025) and upserts Player records.
    public class PlayerRosterImporter
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        public PlayerRosterImporter(AppDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient(); // In production, prefer IHttpClientFactory
        }

        // Imports 40-man rosters for the 30 MLB teams and performs upserts into Players.
        
        public async Task ImportAllPlayersAsync()
        {
            // Cache Teams by name (case-insensitive)
            var teams = await _context.Teams.AsNoTracking().ToListAsync();
            var teamsByName = teams.ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);

            // Cache Players by MLB_Player_Id (tracked, for upsert)
            var existingPlayers = await _context.Players
                .Where(p => p.MLB_Player_Id != null)
                .ToListAsync();

            var playersByMlbId = existingPlayers.ToDictionary(
                p => p.MLB_Player_Id!.Value,
                p => p
            );

            foreach (var kv in PlayerMaps.MlbTeamIdToOfficialName)
            {
                var mlbTeamId = kv.Key;
                var officialTeamName = kv.Value;

                // Ensure Team exists locally
                if (!teamsByName.TryGetValue(officialTeamName, out var team))
                {
                    team = new Team { Name = officialTeamName };
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();
                    teamsByName[officialTeamName] = team;
                }

                // Fetch 40-man roster for the team
                var url = $"https://statsapi.mlb.com/api/v1/teams/{mlbTeamId}/roster?rosterType=40Man&season=2025";
                var json = await _httpClient.GetStringAsync(url);

                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("roster", out var roster) ||
                    roster.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var item in roster.EnumerateArray())
                {
                    // person.id (MLB player id)
                    if (!item.TryGetProperty("person", out var person)) continue;
                    if (!person.TryGetProperty("id", out var idEl)) continue;
                    if (!idEl.TryGetInt64(out var mlbId)) continue;

                    // person.fullName
                    if (!person.TryGetProperty("fullName", out var nameEl)) continue;
                    var fullName = nameEl.GetString();
                    if (string.IsNullOrWhiteSpace(fullName)) continue;

                    // jerseyNumber -> int?
                    int? number = null;
                    if (item.TryGetProperty("jerseyNumber", out var jerseyEl))
                    {
                        var jersey = jerseyEl.GetString();
                        if (!string.IsNullOrWhiteSpace(jersey) && int.TryParse(jersey, out var jerseyNum))
                            number = jerseyNum;
                    }

                    // position.abbreviation (e.g., "P", "C", "1B", ...)
                    string? position = null;
                    if (item.TryGetProperty("position", out var posEl) &&
                        posEl.TryGetProperty("abbreviation", out var abbrEl))
                    {
                        position = abbrEl.GetString();
                    }

                    // status.code (e.g., Active/IL)
                    string? status = null;
                    if (item.TryGetProperty("status", out var statusEl) &&
                        statusEl.TryGetProperty("code", out var codeEl))
                    {
                        status = codeEl.GetString();
                    }

                    // Upsert by MLB_Player_Id
                    if (!playersByMlbId.TryGetValue(mlbId, out var player))
                    {
                        // New tracked entity
                        player = new Player
                        {
                            MLB_Player_Id = mlbId,
                            TeamId = team.Id,
                            Name = fullName!.Trim(),
                            Number = number,
                            Position = position,
                            Status = status
                        };
                        _context.Players.Add(player);

                        // Keep dictionary in sync to avoid duplicates within the same pass
                        playersByMlbId[mlbId] = player;
                    }
                    else
                    {
                        // Existing tracked entity: set properties; Update() is not required
                        player.TeamId = team.Id;
                        player.Name = fullName!.Trim();
                        player.Number = number;
                        player.Position = position;
                        player.Status = status;
                    }
                }

                // Save per team to keep batches small and allow partial progress
                await _context.SaveChangesAsync();
            }
        }
    }
}
