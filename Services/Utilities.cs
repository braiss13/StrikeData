using System.Globalization;
using System.Text;
using HtmlAgilityPack;

namespace StrikeData.Services
{
    /// <summary>
    /// Utility helpers used across scrapers/importers for parsing and normalization.
    /// </summary>
    public static class Utilities
    {
        /// <summary>
        /// Parses a string into float? using invariant culture. Returns null on failure.
        /// Used to convert scraped numeric strings into typed values.
        /// </summary>
        public static float? Parse(string input)
        {
            return float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ? val : null;
        }

        /// <summary>
        /// Cleans HTML-derived text: decodes entities, trims, collapses whitespace,
        /// and removes line/tab controls to produce a single-line string.
        /// </summary>
        public static string CleanText(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var t = HtmlEntity.DeEntitize(s).Trim();
            t = t.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ");
            while (t.Contains("  ")) t = t.Replace("  ", " ");
            return t;
        }

        // ======================
        //  NAME NORMALIZATION
        // ======================

        /// <summary>
        /// Removes diacritics (accents) while preserving base characters.
        /// This supports matching when sources differ in accent use.
        /// </summary>
        private static string RemoveDiacritics(string s)
        {
            var normalized = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(capacity: normalized.Length);
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Strips common suffix tokens (e.g., Jr., Sr., II, III) from the end of a name.
        /// This avoids mismatches where rosters include a suffix and tables do not.
        /// </summary>
        private static string StripSuffixes(string s)
        {
            var tokens = s.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (tokens.Count == 0) return s;

            var suffixes = new HashSet<string>(new[]
            {
                "jr","jr.","sr","sr.","ii","iii","iv","v"
            }, StringComparer.OrdinalIgnoreCase);

            while (tokens.Count > 0 && suffixes.Contains(tokens[^1].TrimEnd('.')))
                tokens.RemoveAt(tokens.Count - 1);

            return string.Join(' ', tokens);
        }

        /// <summary>
        /// Reorders “Last, First” to “First Last”. Leaves other formats unchanged.
        /// </summary>
        private static string ReorderIfComma(string s)
        {
            // "Bichette, Bo" -> "Bo Bichette"
            var parts = s.Split(',', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var last = parts[0].Trim();
                var first = parts[1].Trim();
                if (first.Length > 0 && last.Length > 0)
                    return $"{first} {last}";
            }
            return s;
        }

        /// <summary>
        /// Collapses a doubled token if the token is exactly two identical halves
        /// (e.g., "michaelmichael" -> "michael"). This defends against rare scraper artifacts.
        /// </summary>
        private static string CollapseDoubledSubstring(string token)
        {
            if (string.IsNullOrEmpty(token)) return token;
            if (token.Length % 2 != 0) return token;

            var half = token.Length / 2;
            var left = token.Substring(0, half);
            var right = token.Substring(half);
            if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
                return left;
            return token;
        }

        /// <summary>
        /// Removes immediate repeated tokens in sequence (e.g., "king king" -> "king").
        /// </summary>
        private static IEnumerable<string> DedupAdjacentTokens(IEnumerable<string> tokens)
        {
            string? prev = null;
            foreach (var t in tokens)
            {
                if (prev != null && string.Equals(prev, t, StringComparison.OrdinalIgnoreCase))
                    continue;

                prev = t;
                yield return t;
            }
        }

        /// <summary>
        /// Normalizes a player name to a consistent, comparable form:
        /// 1) Reorders "Last, First" to "First Last"
        /// 2) Removes suffixes (Jr., Sr., II, ...)
        /// 3) Removes diacritics (accents)
        /// 4) Strips punctuation (keeps letters/digits/spaces)
        /// 5) Collapses whitespace
        /// 6) Lowercases tokens and collapses doubled-token artifacts
        /// 7) Deduplicates adjacent tokens
        /// The result improves matching between roster data and scraped tables.
        /// </summary>
        public static string NormalizePlayerName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";

            var s = name.Trim();
            s = s.Replace("“", "\"").Replace("”", "\"").Replace("’", "'").Replace("`", "'");

            // 1) "Last, First" -> "First Last"
            s = ReorderIfComma(s);

            // 2) Drop suffixes
            s = StripSuffixes(s);

            // 3) Remove diacritics
            s = RemoveDiacritics(s);

            // 4) Remove punctuation except spaces
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
                    sb.Append(ch);
            }
            s = sb.ToString();

            // 5) Collapse whitespace
            s = CleanText(s);

            // 6) Token-level cleanup
            var tokens = s.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                          .Select(t => CollapseDoubledSubstring(t.ToLowerInvariant()))
                          .ToList();

            tokens = DedupAdjacentTokens(tokens).ToList();

            // 7) Join normalized tokens
            return string.Join(' ', tokens);
        }
    }
}
