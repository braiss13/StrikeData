using HtmlAgilityPack;

namespace StrikeData.Services.TeamData.Scrapers
{
    /// <summary>
    /// Common base for Baseball Almanac scrapers:
    /// - Issues HTTP GET requests with a desktop user-agent and a valid referrer,
    /// - Loads the HTML into an HtmlDocument for downstream parsing.
    /// </summary>
    public abstract class BaseballAlmanacScraperBase
    {
        protected readonly HttpClient _httpClient;

        protected BaseballAlmanacScraperBase(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Sends a GET request with UA/referrer headers and returns the parsed HtmlDocument.
        /// </summary>
        protected async Task<HtmlDocument> LoadDocumentAsync(string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            // Use a common desktop UA to avoid simplistic bot filters.
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:100.0) Gecko/20100101 Firefox/100.0");
            // Provide a referrer from the same site.
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
