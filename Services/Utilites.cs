using System.Globalization;

namespace StrikeData.Services
{
    public static class Utilites
    {
        
        // Método creado para convertir el String a float (empleado para parsear los datos al final)
        public static float? Parse(string input)
        {
            return float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ? val : null;
        }

    }
}
