namespace StrikeData.Services.StaticMaps
{
    /*
        Static maps consumed by PLAYER importers/scrapers to translate external payloads
        (API fields, HTML headers, ids) into the internal model (categories, types, names).
    */
    public static class PlayerMaps
    {
        // ===========================
        // Fielding (Players)
        // ===========================

        /*
            Canonical set of fielding metric abbreviations used in PlayerStatType and UI.
            Scraped values are aligned to this list.
        */
        public static readonly string[] FieldingMetrics = new[]
        {
            "OUTS","TC","CH","PO","A","E","DP","PB","CASB","CACS","FLD%"
        };

        /*
            Header synonyms as they appear on Baseball Almanac fielding tables.
            Each key is our canonical abbreviation; the value lists acceptable header texts.
            This allows the scraper to locate the correct columns despite header variations.
        */
        public static IReadOnlyDictionary<string, string[]> FieldingHeaderSynonyms { get; } =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Name"] = new[] { "Name", "Player", "Player Name" },
                ["POS"]  = new[] { "POS", "Pos", "Position" },
                ["OUTS"] = new[] { "OUTS", "Outs", "Inn Outs", "INN OUTS", "Inn (Outs)", "Innings (Outs)" },
                ["TC"]   = new[] { "TC", "Total Chances", "Tot Ch", "T. Ch." },
                ["CH"]   = new[] { "CH", "Ch", "Chances" },
                ["PO"]   = new[] { "PO", "Putouts" },
                ["A"]    = new[] { "A", "Assists" },
                ["E"]    = new[] { "E", "Errors" },
                ["DP"]   = new[] { "DP", "Double Plays" },
                ["PB"]   = new[] { "PB", "Passed Balls" },
                ["CASB"] = new[] { "CASB", "SB" },
                ["CACS"] = new[] { "CACS", "CS" },
                ["FLD%"] = new[] { "FLD%", "Fld%", "FPCT", "Fld Pct", "Fielding %" },
            };

        // ===========================
        // MLB team_id -> Official name
        // ===========================

        /*
            MLB stats API team ids mapped to the official team names used in our database.
            Importers use this to connect API ids to the local Team rows by name.
        */
        public static IReadOnlyDictionary<int, string> MlbTeamIdToOfficialName { get; } =
            new Dictionary<int, string>
            {
                { 119, "Los Angeles Dodgers" },
                { 112, "Chicago Cubs" },
                { 147, "New York Yankees" },
                { 109, "Arizona Diamondbacks" },
                { 116, "Detroit Tigers" },
                { 143, "Philadelphia Phillies" },
                { 111, "Boston Red Sox" },
                { 138, "St. Louis Cardinals" },
                { 158, "Milwaukee Brewers" },
                { 113, "Cincinnati Reds" },
                { 136, "Seattle Mariners" },
                { 120, "Washington Nationals" },
                { 121, "New York Mets" },
                { 139, "Tampa Bay Rays" },
                { 135, "San Diego Padres" },
                { 133, "Athletics" },
                { 141, "Toronto Blue Jays" },
                { 137, "San Francisco Giants" },
                { 146, "Miami Marlins" },
                { 144, "Atlanta Braves" },
                { 114, "Cleveland Guardians" },
                { 108, "Los Angeles Angels" },
                { 117, "Houston Astros" },
                { 142, "Minnesota Twins" },
                { 110, "Baltimore Orioles" },
                { 145, "Chicago White Sox" },
                { 140, "Texas Rangers" },
                { 134, "Pittsburgh Pirates" },
                { 118, "Kansas City Royals" },
                { 115, "Colorado Rockies" }
            };

        // ===========================
        // Player JSON field maps (MLB API)
        // ===========================

        /*
            Maps internal stat abbreviations to field names in the MLB player stats API payload.
            Keys match PlayerStatType.Name; values are JSON properties under each "stats" array item.
            Case-insensitive for robustness.
        */
        public static class StatJsonFields
        {
            public static IReadOnlyDictionary<string, string> Pitching { get; } =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    // Basic
                    { "W", "wins" },
                    { "L", "losses" },
                    { "ERA", "era" },
                    { "G", "gamesPlayed" },
                    { "GS", "gamesStarted" },
                    { "CG", "completeGames" },
                    { "SHO", "shutouts" },
                    { "SV", "saves" },
                    { "SVO", "saveOpportunities" },
                    { "IP", "inningsPitched" },
                    { "R", "runs" },
                    { "H", "hits" },
                    { "ER", "earnedRuns" },
                    { "HR", "homeRuns" },
                    { "HB", "hitBatsmen" },
                    { "BB", "baseOnBalls" },
                    { "SO", "strikeOuts" },
                    { "WHIP", "whip" },
                    { "AVG", "avg" },

                    // Advanced
                    { "TBF", "battersFaced" },
                    { "NP", "numberOfPitches" },
                    { "P/IP", "pitchesPerInning" },
                    { "QS", "qualityStarts" },
                    { "GF", "gamesFinished" },
                    { "HLD", "holds" },
                    { "IBB", "intentionalWalks" },
                    { "WP", "wildPitches" },
                    { "BK", "balks" },
                    { "GDP", "groundIntoDoublePlay" },
                    { "GO/AO", "groundOutsToAirouts" },
                    { "SO/9", "strikeoutsPer9Inn" },
                    { "BB/9", "walksPer9Inn" },
                    { "H/9", "hitsPer9Inn" },
                    { "K/BB", "strikeoutWalkRatio" },
                    { "BABIP", "babip" },
                    { "SB", "stolenBases" },
                    { "CS", "caughtStealing" },
                    { "PK", "pickoffs" }
                };

            public static IReadOnlyDictionary<string, string> Hitting { get; } =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "G", "gamesPlayed" },
                    { "AB", "atBats" },
                    { "R", "runs" },
                    { "H", "hits" },
                    { "2B", "doubles" },
                    { "3B", "triples" },
                    { "HR", "homeRuns" },
                    { "RBI", "rbi" },
                    { "BB", "baseOnBalls" },
                    { "SO", "strikeOuts" },
                    { "SB", "stolenBases" },
                    { "CS", "caughtStealing" },
                    { "AVG", "avg" },
                    { "OBP", "obp" },
                    { "SLG", "slg" },
                    { "OPS", "ops" },
                    { "PA", "plateAppearances" },
                    { "HBP", "hitByPitch" },
                    { "SAC", "sacBunts" },
                    { "SF", "sacFlies" },
                    { "GIDP", "gidp" },
                    { "GO/AO", "groundOutsToAirouts" },
                    { "XBH", "extraBaseHits" },
                    { "TB", "totalBases" },
                    { "IBB", "intentionalWalks" },
                    { "BABIP", "babip" },
                    { "ISO", "iso" },
                    { "AB/HR", "atBatsPerHomeRun" },
                    { "BB/K", "walksPerStrikeout" },
                    { "BB%", "walksPerPlateAppearance" },
                    { "SO%", "strikeoutsPerPlateAppearance" },
                    { "HR%", "homeRunsPerPlateAppearance" }
                };
        }
    }
}
