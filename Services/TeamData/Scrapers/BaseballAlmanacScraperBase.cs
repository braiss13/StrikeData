using HtmlAgilityPack;

namespace StrikeData.Services.TeamData.Scrapers
{
    /// <summary>
    /// Base común para scrapers de Baseball-Almanac:
    /// - Crea HttpRequest con UA/referrer
    /// - Descarga HTML y devuelve HtmlDocument
    /// </summary>
    public abstract class BaseballAlmanacScraperBase
    {
        protected readonly HttpClient _httpClient;

        protected BaseballAlmanacScraperBase(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        protected async Task<HtmlDocument> LoadDocumentAsync(string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:100.0) Gecko/20100101 Firefox/100.0");
            request.Headers.Referrer = new System.Uri("https://www.baseball-almanac.com/teammenu.shtml");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc;
        }
    }
}
