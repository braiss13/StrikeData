namespace StrikeData.Services.StaticMaps
{
    /// <summary>
    /// Static URL maps for TeamRankings (team-level metrics and trend pages).
    /// Keys are the internal abbreviations used in the UI; values are full URLs.
    /// </summary>
    public static class TeamRankingsMaps
    {
        // Base URL fragments used to build complete endpoints
        private const string BASE = "https://www.teamrankings.com/mlb";
        private const string STAT_BASE = BASE + "/stat/";
        private const string WIN_TRENDS_BASE = BASE + "/trends/win_trends/?sc=";

        /// <summary>
        /// Helper to build a stat map (key -> full stat URL) from a slug dictionary.
        /// </summary>
        private static IReadOnlyDictionary<string, string> BuildStatMap(IDictionary<string, string> slugByKey) =>
            slugByKey.ToDictionary(kv => kv.Key, kv => STAT_BASE + kv.Value, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Helper to build a trends map (key -> full trends URL) from a query-string code map.
        /// </summary>
        private static IReadOnlyDictionary<string, string> BuildTrendsMap(IDictionary<string, string> scByKey) =>
            scByKey.ToDictionary(kv => kv.Key, kv => WIN_TRENDS_BASE + kv.Value, StringComparer.OrdinalIgnoreCase);

        // ========== Curious Facts ==========
        /// <summary>
        /// Team-level “curious facts” metrics (per-inning splits and YRFI/NRFI).
        /// </summary>
        public static IReadOnlyDictionary<string, string> CuriousFacts { get; } = BuildStatMap(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "YRFI",     "yes-run-first-inning-pct" },
                { "NRFI",     "no-run-first-inning-pct" },
                { "OYRFI",    "opponent-yes-run-first-inning-pct" },
                { "ONRFI",    "opponent-no-run-first-inning-pct" },
                { "1IR/G",    "1st-inning-runs-per-game" },
                { "2IR/G",    "2nd-inning-runs-per-game" },
                { "3IR/G",    "3rd-inning-runs-per-game" },
                { "4IR/G",    "4th-inning-runs-per-game" },
                { "5IR/G",    "5th-inning-runs-per-game" },
                { "6IR/G",    "6th-inning-runs-per-game" },
                { "7IR/G",    "7th-inning-runs-per-game" },
                { "8IR/G",    "8th-inning-runs-per-game" },
                { "9IR/G",    "9th-inning-runs-per-game" },
                { "XTRAIR/G", "extra-inning-runs-per-game" },
                { "O1IR/G",   "opponent-1st-inning-runs-per-game" },
                { "O2IR/G",   "opponent-2nd-inning-runs-per-game" },
                { "O3IR/G",   "opponent-3rd-inning-runs-per-game" },
                { "O4IR/G",   "opponent-4th-inning-runs-per-game" },
                { "O5IR/G",   "opponent-5th-inning-runs-per-game" },
                { "O6IR/G",   "opponent-6th-inning-runs-per-game" },
                { "O7IR/G",   "opponent-7th-inning-runs-per-game" },
                { "O8IR/G",   "opponent-8th-inning-runs-per-game" },
                { "O9IR/G",   "opponent-9th-inning-runs-per-game" },
                { "OXTRAIR/G","opponent-extra-inning-runs-per-game" },
                { "F4IR/G",   "first-4-innings-runs-per-game" },
                { "F5IR/G",   "first-5-innings-runs-per-game" },
                { "F6IR/G",   "first-6-innings-runs-per-game" },
                { "OF4IR/G",  "opponent-first-4-innings-runs-per-game" },
                { "OF5IR/G",  "opponent-first-5-innings-runs-per-game" },
                { "OF6IR/G",  "opponent-first-6-innings-runs-per-game" },
                { "L2IR/G",   "last-2-innings-runs-per-game" },
                { "L3IR/G",   "last-3-innings-runs-per-game" },
                { "L4IR/G",   "last-4-innings-runs-per-game" },
                { "OL2IR/G",  "opponent-last-2-innings-runs-per-game" },
                { "OL3IR/G",  "opponent-last-3-innings-runs-per-game" },
                { "OL4IR/G",  "opponent-last-4-innings-runs-per-game" },
            });

        // ========== Fielding ==========
        /// <summary>
        /// Team-level fielding endpoints. Keys match the UI abbreviations.
        /// </summary>
        public static IReadOnlyDictionary<string, string> Fielding { get; } = BuildStatMap(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "DP", "double-plays-per-game" },
                { "E",  "runs-per-game" } // kept as originally provided
            });

        // ========== Hitting ==========
        /// <summary>
        /// Team-level hitting endpoints. Abbreviations align with the glossary.
        /// </summary>
        public static IReadOnlyDictionary<string, string> Hitting { get; } = BuildStatMap(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "AB",   "at-bats-per-game" },
                { "R",    "runs-per-game" },
                { "H",    "hits-per-game" },
                { "HR",   "home-runs-per-game" },
                { "S",    "singles-per-game" },
                { "2B",   "doubles-per-game" },
                { "3B",   "triples-per-game" },
                { "RBI",  "rbis-per-game" },
                { "BB",   "walks-per-game" },
                { "SO",   "strikeouts-per-game" },
                { "SB",   "stolen-bases-per-game" },
                { "SBA",  "stolen-bases-attempted-per-game" },
                { "CS",   "caught-stealing-per-game" },
                { "SAC",  "sacrifice-hits-per-game" },
                { "SF",   "sacrifice-flys-per-game" },
                { "LOB",  "left-on-base-per-game" },
                { "TLOB", "team-left-on-base-per-game" },
                { "HBP",  "hit-by-pitch-per-game" },
                { "GIDP", "grounded-into-double-plays-per-game" },
                { "RLSP", "runners-left-in-scoring-position-per-game" },
                { "TB",   "total-bases-per-game" },
                { "AVG",  "batting-average" },
                { "SLG",  "slugging-pct" },
                { "OBP",  "on-base-pct" },
                { "OPS",  "on-base-plus-slugging-pct" },
                { "AB/HR","at-bats-per-home-run" },
            });

        // ========== Pitching ==========
        /// <summary>
        /// Team-level pitching endpoints. Keys are internal abbreviations.
        /// </summary>
        public static IReadOnlyDictionary<string, string> Pitching { get; } = BuildStatMap(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "OP/G", "outs-pitched-per-game" },
                { "ER/G", "outs-pitched-per-game" },
                { "SO/9", "strikeouts-per-9" },
                { "H/9",  "home-runs-per-9" },
                { "HR/9", "walks-per-9" },
                { "W/9",  "walks-per-9" }
            });

        // ========== Win Trends ==========
        /// <summary>
        /// Win-trend contexts built by attaching a query-string short code (sc).
        /// </summary>
        public static IReadOnlyDictionary<string, string> WinTrends { get; } = BuildTrendsMap(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "All Games", "all_games" },
                { "After Win", "is_after_win" },
                { "After Loss", "is_after_loss" },
                { "League Games", "is_league" },
                { "Non League Games", "non_league" },
                { "Division Games", "is_division" },
                { "Non Division Games", "non_division" },
                { "As Home", "s_home" },
                { "As Away", "is_away" },
                { "As Favorite", "is_fav" },
                { "As Underdog", "is_dog" },
                { "As Home Favorite", "is_home_fav" },
                { "As Home Underdog", "is_home_dog" },
                { "As Away Favorite", "is_away_favs" },
                { "As Away Underdog", "is_away_dog" },
                { "With No Rest", "no_rest" },
                { "1 Day Off", "one_day_off" },
                { "4+ Days Off", "four_plus_days_off" },
                { "Second Game of Doubleheader", "s_doubleheader" },
                { "With Rest Advantage", "rest_advantage" },
                { "With Rest Disadvantage", "rest_disadvantage" },
                { "Equal Rest", "equal_rest" },
            });
    }
}
