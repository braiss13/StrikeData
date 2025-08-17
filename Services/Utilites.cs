using System.Globalization;
using HtmlAgilityPack;

namespace StrikeData.Services
{
    public static class Utilites
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

    }
}
