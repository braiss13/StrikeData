using HtmlAgilityPack;
using StrikeData.Services.TeamData.Scrapers;
using StrikeData.Services.StaticMaps;

namespace StrikeData.Services.PlayerData
{
    /*
        Scrapes per-player fielding tables from Baseball Almanac for a team and season.
        Produces normalized rows containing Name, optional POS, and a map of metrics.
    */
    public class PlayerFieldingScraper : BaseballAlmanacScraperBase
    {
        public PlayerFieldingScraper(HttpClient httpClient) : base(httpClient) { }

        public class PlayerFieldingRowDto
        {
            public string Name { get; set; } = "";
            public string? Pos { get; set; }
            public Dictionary<string, float?> Values { get; set; } =
                new(StringComparer.OrdinalIgnoreCase);
        }

        /*
            Downloads and parses the best matching fielding table for a team.
            Heuristics select the table whose header hits the most expected columns.
        */
        public async Task<List<PlayerFieldingRowDto>> GetTeamFieldingRowsAsync(string teamCode, int year)
        {
            var url = $"https://www.baseball-almanac.com/teamstats/fielding.php?y={year}&t={teamCode.ToUpperInvariant()}";
            var doc = await LoadDocumentAsync(url);

            // Baseball Almanac pages may contain multiple tables; scan all of them.
            var tables = doc.DocumentNode.SelectNodes("//table") ?? new HtmlNodeCollection(null);
            if (tables.Count == 0) return new List<PlayerFieldingRowDto>();

            // Target metric headers care about (abbreviations)
            var wanted = new[] { "OUTS", "TC", "CH", "PO", "A", "E", "DP", "PB", "CASB", "CACS", "FLD%" };

            // Track the "best" table/header mapping according to header coverage ("hits")
            HtmlNode? bestTable = null;
            int bestHeaderRowIdx = -1;
            Dictionary<string, int>? bestIndexMap = null;
            int bestHits = -1;

            // Explore each table: find a header row that includes "Name" and map indices
            for (int tIdx = 0; tIdx < tables.Count; tIdx++)
            {
                var table = tables[tIdx];
                var rows = table.SelectNodes(".//tr");
                if (rows == null || rows.Count < 2) continue;

                int headerRowIndex = -1;
                List<string>? headerCellsNorm = null;

                // Detect header row: it must contain a recognized "Name" header synonym
                for (int r = 0; r < rows.Count; r++)
                {
                    var hc = rows[r].SelectNodes("th|td")
                                    ?.Select(h => Utilities.CleanText(h.InnerText))
                                    .ToList();
                    if (hc == null || hc.Count == 0) continue;

                    bool hasName = PlayerMaps.FieldingHeaderSynonyms["Name"].Any(syn =>
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

                // Map the "Name" column (required)
                int nameIdx = -1;
                foreach (var syn in PlayerMaps.FieldingHeaderSynonyms["Name"])
                {
                    int idx = headerCellsNorm.FindIndex(s => s.Equals(syn, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0) { nameIdx = idx; break; }
                }
                if (nameIdx < 0) continue;
                indexMap["Name"] = nameIdx;

                // Map the "POS" column if present (optional)
                foreach (var syn in PlayerMaps.FieldingHeaderSynonyms["POS"])
                {
                    int idx = headerCellsNorm.FindIndex(s => s.Equals(syn, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0) { indexMap["POS"] = idx; break; }
                }

                // Count how many target metrics this header exposes
                int hits = 0;
                foreach (var w in wanted)
                {
                    int idx = -1;
                    foreach (var syn in PlayerMaps.FieldingHeaderSynonyms[w])
                    {
                        idx = headerCellsNorm.FindIndex(s => s.Equals(syn, StringComparison.OrdinalIgnoreCase));
                        if (idx >= 0) break;
                    }
                    if (idx >= 0) { indexMap[w] = idx; hits++; }
                }

                // Keep the table with the most metrics found
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
                Console.WriteLine("[FieldingScraper][WARN] No valid fielding table was found.");
                return new List<PlayerFieldingRowDto>();
            }

            // Parse data rows after the detected header row
            var data = new List<PlayerFieldingRowDto>();
            var allRows = bestTable.SelectNodes(".//tr") ?? new HtmlNodeCollection(null);

            for (int r = bestHeaderRowIdx + 1; r < allRows.Count; r++)
            {
                var cells = allRows[r].SelectNodes("td");
                if (cells == null || cells.Count == 0) continue;

                if (!bestIndexMap.TryGetValue("Name", out int nameIdx)) continue;
                if (nameIdx < 0 || nameIdx >= cells.Count) continue;

                // Stop on subheaders/totals: header rows sometimes appear again below
                var ths = allRows[r].SelectNodes("th");
                if (ths != null && ths.Count > 0) break;

                // Use anchor text when available (names often wrapped in <a>)
                var nameCell = cells[nameIdx];
                var anchorTexts = nameCell.SelectNodes(".//a")
                                          ?.Select(a => Utilities.CleanText(a.InnerText))
                                          .Where(t => !string.IsNullOrWhiteSpace(t))
                                          .ToList();

                var name = anchorTexts != null && anchorTexts.Count > 0
                    ? string.Join(" ", anchorTexts)
                    : Utilities.CleanText(nameCell.InnerText);

                // Bail out on totals/footer rows
                if (string.IsNullOrWhiteSpace(name) ||
                    name.Equals("Totals", StringComparison.OrdinalIgnoreCase))
                    break;

                var dto = new PlayerFieldingRowDto { Name = name };

                // Extract POS if the column exists
                if (bestIndexMap.TryGetValue("POS", out int posIdx) &&
                    posIdx >= 0 && posIdx < cells.Count)
                {
                    dto.Pos = Utilities.CleanText(cells[posIdx].InnerText);
                }

                // Extract each mapped metric cell, normalize, and parse to float?
                foreach (var kv in bestIndexMap)
                {
                    var key = kv.Key;
                    if (key.Equals("Name", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("POS", StringComparison.OrdinalIgnoreCase))
                        continue;

                    int idx = kv.Value;
                    if (idx < 0 || idx >= cells.Count) continue;

                    var raw = Utilities.CleanText(cells[idx].InnerText)
                                       .Replace("%", "")
                                       .Replace(",", "")
                                       .Trim();

                    dto.Values[key] = Utilities.Parse(raw);
                }

                data.Add(dto);
            }

            return data;
        }
    }
}
