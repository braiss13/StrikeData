using System.Globalization;
using System.Text;
using HtmlAgilityPack;

namespace StrikeData.Services
{
    public static class Utilities
    {
        // Método creado para convertir el String a float (empleado para parsear los datos al final)
        public static float? Parse(string input)
        {
            return float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ? val : null;
        }

        // Método para limpiar texto: quita entidades HTML, espacios extra y normaliza
        public static string CleanText(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var t = HtmlEntity.DeEntitize(s).Trim();
            t = t.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ");
            while (t.Contains("  ")) t = t.Replace("  ", " ");
            return t;
        }

        // ======================
        //  NORMALIZACIÓN NOMBRES
        // ======================

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

        // "michaelmichael" -> "michael", "bobo" -> "bo"
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

        // "king king" -> "king"
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

        public static string NormalizePlayerName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";

            var s = name.Trim();
            s = s.Replace("“", "\"").Replace("”", "\"").Replace("’", "'").Replace("`", "'");

            // 1) "Apellido, Nombre" -> "Nombre Apellido"
            s = ReorderIfComma(s);

            // 2) quitar sufijos
            s = StripSuffixes(s);

            // 3) quitar diacríticos
            s = RemoveDiacritics(s);

            // 4) quitar puntuación salvo espacios
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
                    sb.Append(ch);
            }
            s = sb.ToString();

            // 5) colapsar espacios
            s = CleanText(s);

            // 6) dividir tokens, colapsar tokens doblados y duplicados adyacentes
            var tokens = s.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                          .Select(t => CollapseDoubledSubstring(t.ToLowerInvariant()))
                          .ToList();

            tokens = DedupAdjacentTokens(tokens).ToList();

            // 7) unir final
            return string.Join(' ', tokens);
        }
    }
}
