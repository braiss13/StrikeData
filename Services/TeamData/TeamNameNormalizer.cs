namespace StrikeData.Services.TeamData
{
    public static class TeamNameNormalizer
    {
        private static readonly Dictionary<string, string> TeamNameMap = new()
        {
            { "LA Dodgers", "Los Angeles Dodgers" },
            { "Chi Cubs", "Chicago Cubs" },
            { "NY Yankees", "New York Yankees" },
            { "Arizona", "Arizona Diamondbacks" },
            { "Detroit", "Detroit Tigers" },
            { "Philadelphia", "Philadelphia Phillies" },
            { "Boston", "Boston Red Sox" },
            { "St. Louis", "St. Louis Cardinals" },
            { "Milwaukee", "Milwaukee Brewers" },
            { "Cincinnati", "Cincinnati Reds" },
            { "Seattle", "Seattle Mariners" },
            { "Washington", "Washington Nationals" },
            { "NY Mets", "New York Mets" },
            { "Tampa Bay", "Tampa Bay Rays" },
            { "San Diego", "San Diego Padres" },
            { "Sacramento", "Athletics" }, // Asumido, ya que no hay Sacramento en MLB; posiblemente Oakland Athletics
            { "Toronto", "Toronto Blue Jays" },
            { "SF Giants", "San Francisco Giants" },
            { "Miami", "Miami Marlins" },
            { "Atlanta", "Atlanta Braves" },
            { "Cleveland", "Cleveland Guardians" },
            { "LA Angels", "Los Angeles Angels" },
            { "Houston", "Houston Astros" },
            { "Minnesota", "Minnesota Twins" },
            { "Baltimore", "Baltimore Orioles" },
            { "Chi Sox", "Chicago White Sox" },
            { "Texas", "Texas Rangers" },
            { "Pittsburgh", "Pittsburgh Pirates" },
            { "Kansas City", "Kansas City Royals" },
            { "Colorado", "Colorado Rockies" }
        };

        public static string Normalize(string name)
        {
            return TeamNameMap.TryGetValue(name, out var officialName) ? officialName : name;
        }
    }
}
