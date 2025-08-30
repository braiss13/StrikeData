namespace StrikeData.Services.StaticMaps
{
    /// <summary>
    /// Mapping from Baseball Almanac team codes to the official team names used in the DB.
    /// This is reused by team and player importers/scrapers to resolve teams consistently.
    /// </summary>
    public static class TeamCodeMap
    {
        public static readonly Dictionary<string, string> CodeToName = new()
        {
            ["TOR"] = "Toronto Blue Jays",
            ["BOS"] = "Boston Red Sox",
            ["NYA"] = "New York Yankees",
            ["TBR"] = "Tampa Bay Rays",
            ["BAL"] = "Baltimore Orioles",
            ["DET"] = "Detroit Tigers",
            ["CLG"] = "Cleveland Guardians",
            ["KCA"] = "Kansas City Royals",
            ["MIN"] = "Minnesota Twins",
            ["CHA"] = "Chicago White Sox",
            ["HOA"] = "Houston Astros",
            ["SEA"] = "Seattle Mariners",
            ["TEX"] = "Texas Rangers",
            ["ANG"] = "Los Angeles Angels",
            ["ATH"] = "Athletics",
            ["PHI"] = "Philadelphia Phillies",
            ["NYN"] = "New York Mets",
            ["MIA"] = "Miami Marlins",
            ["ATL"] = "Atlanta Braves",
            ["WS0"] = "Washington Nationals",
            ["ML4"] = "Milwaukee Brewers",
            ["CHN"] = "Chicago Cubs",
            ["CN5"] = "Cincinnati Reds",
            ["SLN"] = "St. Louis Cardinals",
            ["PIT"] = "Pittsburgh Pirates",
            ["SDN"] = "San Diego Padres",
            ["LAN"] = "Los Angeles Dodgers",
            ["ARI"] = "Arizona Diamondbacks",
            ["SFN"] = "San Francisco Giants",
            ["COL"] = "Colorado Rockies"
        };
    }
}
