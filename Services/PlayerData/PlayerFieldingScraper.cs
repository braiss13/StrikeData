// PlayerFieldingScraper.cs
using HtmlAgilityPack;
using StrikeData.Services.Common;

namespace StrikeData.Services.PlayerData
{
    public class PlayerFieldingScraper : BaseballAlmanacScraperBase
    {
        public PlayerFieldingScraper(HttpClient httpClient) : base(httpClient) { }

        public class PlayerFieldingRowDto
        {
            public string Name { get; set; } = "";
            public string? Pos { get; set; }             // <-- NUEVO
            public Dictionary<string, float?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private static readonly Dictionary<string, string[]> HeaderSynonyms = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Name"] = new[] { "Name", "Player", "Player Name" },
            ["POS"]  = new[] { "POS", "Pos", "Position" },  // Se coge también la posición para quedarse solo con una (el mismo jugador puede tener varias)
            ["OUTS"] = new[] { "OUTS", "Outs", "Inn Outs", "INN OUTS", "Inn (Outs)", "Innings (Outs)" },
            ["TC"]   = new[] { "TC", "Total Chances", "Tot Ch", "T. Ch." },
            ["CH"]   = new[] { "CH", "Ch", "Chances" },
            ["PO"]   = new[] { "PO", "Putouts" },
            ["A"]    = new[] { "A", "Assists" },
            ["E"]    = new[] { "E", "Errors" },
            ["DP"]   = new[] { "DP", "Double Plays" },
            ["PB"]   = new[] { "PB", "Passed Balls" },
            ["CASB"] = new[] { "CASB", "SB" },
            ["CACS"] = new[] { "CACS", "CS" },
            ["FLD%"] = new[] { "FLD%", "Fld%", "FPCT", "Fld Pct", "Fielding %" },
        };

        public async Task<List<PlayerFieldingRowDto>> GetTeamFieldingRowsAsync(string teamCode, int year)
        {
            var url = $"https://www.baseball-almanac.com/teamstats/fielding.php?y={year}&t={teamCode.ToUpperInvariant()}";
            Console.WriteLine($"[FieldingScraper] GET {url}");

            var doc = await LoadDocumentAsync(url);

            var tables = doc.DocumentNode.SelectNodes("//table") ?? new HtmlNodeCollection(null);
            if (tables.Count == 0) return new List<PlayerFieldingRowDto>();

            var wanted = new[] { "OUTS","TC","CH","PO","A","E","DP","PB","CASB","CACS","FLD%" };

            HtmlNode? bestTable = null;
            int bestHeaderRowIdx = -1;
            Dictionary<string, int>? bestIndexMap = null;
            int bestHits = -1;

            for (int tIdx = 0; tIdx < tables.Count; tIdx++)
            {
                var table = tables[tIdx];
                var rows = table.SelectNodes(".//tr");
                if (rows == null || rows.Count < 2) continue;

                int headerRowIndex = -1;
                List<string>? headerCellsNorm = null;

                for (int r = 0; r < rows.Count; r++)
                {
                    var hc = rows[r].SelectNodes("th|td")?.Select(h => Utilities.CleanText(h.InnerText)).ToList();
                    if (hc == null || hc.Count == 0) continue;

                    bool hasName = HeaderSynonyms["Name"].Any(syn =>
                        hc.Any(x => x.Equals(syn, StringComparison.OrdinalIgnoreCase)));

                    if (hasName)
                    {
                        headerRowIndex = r;
                        headerCellsNorm = hc;
                        break;
                    }
                }

                if (headerRowIndex < 0 || headerCellsNorm == null) continue;

                var indexMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                // Name
                int nameIdx = -1;
                foreach (var syn in HeaderSynonyms["Name"])
                {
                    int idx = headerCellsNorm.FindIndex(s => s.Equals(syn, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0) { nameIdx = idx; break; }
                }
                if (nameIdx < 0) continue;
                indexMap["Name"] = nameIdx;

                // POS (opcional)
                foreach (var syn in HeaderSynonyms["POS"])
                {
                    int idx = headerCellsNorm.FindIndex(s => s.Equals(syn, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0) { indexMap["POS"] = idx; break; }
                }

                int hits = 0;
                foreach (var w in wanted)
                {
                    int idx = -1;
                    foreach (var syn in HeaderSynonyms[w])
                    {
                        idx = headerCellsNorm.FindIndex(s => s.Equals(syn, StringComparison.OrdinalIgnoreCase));
                        if (idx >= 0) break;
                    }
                    if (idx >= 0) { indexMap[w] = idx; hits++; }
                }

                if (hits > bestHits)
                {
                    bestHits = hits;
                    bestTable = table;
                    bestHeaderRowIdx = headerRowIndex;
                    bestIndexMap = indexMap;
                }
            }

            if (bestTable == null || bestIndexMap == null)
            {
                Console.WriteLine("[FieldingScraper][WARN] No se encontró una tabla de fielding válida.");
                return new List<PlayerFieldingRowDto>();
            }

            var data = new List<PlayerFieldingRowDto>();
            var allRows = bestTable.SelectNodes(".//tr") ?? new HtmlNodeCollection(null);

            for (int r = bestHeaderRowIdx + 1; r < allRows.Count; r++)
            {
                var cells = allRows[r].SelectNodes("td");
                if (cells == null || cells.Count == 0) continue;

                if (!bestIndexMap.TryGetValue("Name", out int nameIdx)) continue;
                if (nameIdx < 0 || nameIdx >= cells.Count) continue;

                var ths = allRows[r].SelectNodes("th");
                if (ths != null && ths.Count > 0) break;

                // Nombre desde <a>
                var nameCell = cells[nameIdx];
                var anchorTexts = nameCell.SelectNodes(".//a")?.Select(a => Utilities.CleanText(a.InnerText))
                                          .Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
                var name = anchorTexts != null && anchorTexts.Count > 0
                    ? string.Join(" ", anchorTexts)
                    : Utilities.CleanText(nameCell.InnerText);
                if (string.IsNullOrWhiteSpace(name) || name.Equals("Totals", StringComparison.OrdinalIgnoreCase)) break;

                var dto = new PlayerFieldingRowDto { Name = name };

                // POS si existe
                if (bestIndexMap.TryGetValue("POS", out int posIdx) && posIdx >= 0 && posIdx < cells.Count)
                {
                    dto.Pos = Utilities.CleanText(cells[posIdx].InnerText);
                }

                // Métricas
                foreach (var kv in bestIndexMap)
                {
                    var key = kv.Key;
                    if (key.Equals("Name", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("POS", StringComparison.OrdinalIgnoreCase))
                        continue;

                    int idx = kv.Value;
                    if (idx < 0 || idx >= cells.Count) continue;

                    var raw = Utilities.CleanText(cells[idx].InnerText).Replace("%", "").Replace(",", "").Trim();
                    dto.Values[key] = Utilities.Parse(raw);
                }

                data.Add(dto);
            }
            
            return data;
        }
    }
}
