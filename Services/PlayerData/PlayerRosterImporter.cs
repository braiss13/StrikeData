using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;

namespace StrikeData.Services.PlayerData
{
    public class PlayerRosterImporter
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        // MLB team_id -> nombre oficial del equipo (como lo guardas en DB)
        private static readonly Dictionary<int, string> _TeamIdMap = new()
        {
            { 119, "Los Angeles Dodgers" },
            { 112, "Chicago Cubs" },
            { 147, "New York Yankees" },
            { 109, "Arizona Diamondbacks" },
            { 116, "Detroit Tigers" },
            { 143, "Philadelphia Phillies" },
            { 111, "Boston Red Sox" },
            { 138, "St. Louis Cardinals" },
            { 158, "Milwaukee Brewers" },
            { 113, "Cincinnati Reds" },
            { 136, "Seattle Mariners" },
            { 120, "Washington Nationals" },
            { 121, "New York Mets" },
            { 139, "Tampa Bay Rays" },
            { 135, "San Diego Padres" },
            { 133, "Athletics" },
            { 141, "Toronto Blue Jays" },
            { 137, "San Francisco Giants" },
            { 146, "Miami Marlins" },
            { 144, "Atlanta Braves" },
            { 114, "Cleveland Guardians" },
            { 108, "Los Angeles Angels" },
            { 117, "Houston Astros" },
            { 142, "Minnesota Twins" },
            { 110, "Baltimore Orioles" },
            { 145, "Chicago White Sox" },
            { 140, "Texas Rangers" },
            { 134, "Pittsburgh Pirates" },
            { 118, "Kansas City Royals" },
            { 115, "Colorado Rockies" }
        };

        public PlayerRosterImporter(AppDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
        }

        /// Importa el 40-man roster de los 30 equipos (temporada 2025) y hace upsert de Players.
        public async Task ImportAllPlayersAsync()
        {
            // Cache de Teams por nombre (case-insensitive)
            var teams = await _context.Teams.AsNoTracking().ToListAsync();
            var teamsByName = teams.ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);

            // Cache de Players por MLB_Player_Id (solo los que lo tienen)
            var existingPlayers = await _context.Players
                .Where(p => p.MLB_Player_Id != null)
                .AsNoTracking()
                .ToListAsync();
            var playersByMlbId = existingPlayers.ToDictionary(p => p.MLB_Player_Id!.Value, p => p);

            foreach (var kv in _TeamIdMap)
            {
                int mlbTeamId = kv.Key;
                string officialTeamName = kv.Value;

                // Asegurar el Team
                if (!teamsByName.TryGetValue(officialTeamName, out var team))
                {
                    team = new Team { Name = officialTeamName };
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();
                    teamsByName[officialTeamName] = team;
                }

                // Importar roster del equipo
                string url = $"https://statsapi.mlb.com/api/v1/teams/{mlbTeamId}/roster?rosterType=40Man&season=2025";
                string json = await _httpClient.GetStringAsync(url);

                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("roster", out var roster) || roster.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var item in roster.EnumerateArray())
                {
                    // person.id (MLB_Player_Id)
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

                    // position.abbreviation
                    string? position = null;
                    if (item.TryGetProperty("position", out var posEl) &&
                        posEl.TryGetProperty("abbreviation", out var abbrEl))
                    {
                        position = abbrEl.GetString();
                    }

                    // status.code
                    string? status = null;
                    if (item.TryGetProperty("status", out var statusEl) &&
                        statusEl.TryGetProperty("code", out var codeEl))
                    {
                        status = codeEl.GetString();
                    }

                    // Upsert por MLB_Player_Id
                    if (!playersByMlbId.TryGetValue(mlbId, out var player))
                    {
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
                        playersByMlbId[mlbId] = player;
                    }
                    else
                    {
                        player.TeamId = team.Id;
                        player.Name = fullName!.Trim();
                        player.Number = number;
                        player.Position = position;
                        player.Status = status;

                        _context.Players.Update(player);
                    }
                }

                // Guarda por equipo (reduce tamaño de lote y deja progreso parcial)
                await _context.SaveChangesAsync();
            }
        }
    }
}
