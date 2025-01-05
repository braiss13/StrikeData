using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Collections.Generic;

namespace StrikeData.Services
{
    public class TeamRankingScraper
    {
        public async Task<List<List<string>>> ScrapeTable(string url)
        {
            // Crear cliente HTTP
            HttpClient client = new HttpClient();

            try
            {
                // Descargar contenido HTML
                var response = await client.GetStringAsync(url);

                // Cargar HTML con HtmlAgilityPack
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(response);

                // Buscar la tabla que contiene los datos
                // Esto indica que buscamos cualquier <table> que tenga la clase CSS datatable. En el HTML de las URLs proporcionadas, las tablas que contienen los datos tienen esta clase.
                var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'datatable')]");

                if (table == null)
                {
                    throw new Exception("No se encontró la tabla en la página.");
                }

                // Obtener las filas de la tabla (tr)
                var rows = table.SelectNodes(".//tr");
                var result = new List<List<string>>();

                foreach (var row in rows)
                {
                    // Obtener las celdas de cada fila (td)
                    var cells = row.SelectNodes(".//td");
                    if (cells != null)
                    {
                        var rowData = new List<string>();
                        foreach (var cell in cells)
                        {
                            // Extraer texto de cada celda
                            rowData.Add(cell.InnerText.Trim());
                        }
                        result.Add(rowData);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al hacer scraping: {ex.Message}");
                return new List<List<string>>(); // Retorna una lista vacía en caso de error
            }
        }
    }
}
