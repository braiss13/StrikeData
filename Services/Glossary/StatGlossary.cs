namespace StrikeData.Services.Glossary
{
    /// <summary>
    /// Distinct stat domains. Some abbreviations exist in multiple domains,
    /// so the domain disambiguates the meaning (e.g., "AVG" team vs player).
    /// </summary>
    public enum StatDomain
    {
        TeamHitting,
        TeamPitching,
        TeamFielding,
        PlayerHitting,
        PlayerPitching,
        PlayerFielding,
        CuriousFacts
    }

    /// <summary>
    /// Display metadata for a stat: long name and user-facing description.
    /// </summary>
    public record StatText(string LongName, string Description);

    /// <summary>
    /// Centralized glossary providing long names and descriptions per domain/abbr.
    /// Views retrieve this to render tooltips and human-readable headings.
    /// </summary>
    public static class StatGlossary
    {
        // Per-domain maps. Keys are case-insensitive on abbreviations to match UI/API.
        private static readonly IReadOnlyDictionary<StatDomain, IReadOnlyDictionary<string, StatText>> _maps =
            new Dictionary<StatDomain, IReadOnlyDictionary<string, StatText>>
        {
            [StatDomain.TeamHitting] = new Dictionary<string, StatText>(StringComparer.OrdinalIgnoreCase)
            {
                ["AVG"]   = new("Batting average", "Hits divided by at-bats (H/AB)"),
                ["OBP"]   = new("On-base percentage", "(H + BB + HBP) / (AB + BB + HBP + SF)"),
                ["SLG"]   = new("Slugging percentage", "Total bases per at-bat"),
                ["OPS"]   = new("On-base plus slugging", "OBP + SLG"),
                ["R"]     = new("Runs scored", "Number of times a player safely reaches home plate."),
                ["H"]     = new("Hits", "Safely reaching base on a fair ball without an error or fielder's choice."),
                ["HR"]    = new("Home runs", "Hits where the batter circles all bases in one play."),
                ["RBI"]   = new("Runs batted in", "Runs scored as a result of the batter's at-bat (excluding errors)."),
                ["BB"]    = new("Walks", "Plate appearances resulting in four balls."),
                ["SO"]    = new("Strikeouts", "Outs recorded on strike three."),
                ["SB"]    = new("Stolen bases", "Bases taken successfully without a hit or error."),
                ["CS"]    = new("Caught stealing", "Runners thrown out while attempting to steal."),
                ["2B"]    = new("Doubles", "Hits on which the batter reaches second base."),
                ["3B"]    = new("Triples", "Hits on which the batter reaches third base."),
                ["AB"]    = new("At-bats", "Plate appearances excluding walks, HBP, sacrifices, interference."),
                ["AB/HR"] = new("At-bats per home run", "AB divided by HR."),
                ["GIDP"]  = new("Ground into double play", "Ground ball that results in more than one out."),
                ["HBP"]   = new("Hit by pitch", "Batter is awarded first base after being hit by a pitch."),
                ["LOB"]   = new("Left on base", "Runners left on base at the end of an inning."),
                ["RLSP"]  = new("Runners left in scoring position", "Average runners left on 2B/3B per game."),
                ["S"]     = new("Singles", "Hits where the batter reaches first base."),
                ["SAC"]   = new("Sacrifice hits", "Bunts that advance a runner while the batter is out."),
                ["SBA"]   = new("Stolen base attempts", "Times a player attempts to steal, successful or caught."),
                ["SF"]    = new("Sacrifice flies", "Fly balls caught for outs that allow a run to score."),
                ["TB"]    = new("Total bases", "1 for single, 2 for double, 3 for triple, 4 for HR."),
                ["TLOB"]  = new("Total left on base", "Total runners left on base at the end of each half-inning")
            },

            [StatDomain.TeamPitching] = new Dictionary<string, StatText>(StringComparer.OrdinalIgnoreCase)
            {
                ["ERA"]  = new("Earned Run Average", "Earned runs per nine innings ((ER × 9) / IP)."),
                ["SHO"]  = new("Shutouts", "Complete games with zero runs allowed."),
                ["CG"]   = new("Complete Games", "Starts where the pitcher throws the entire game."),
                ["SV"]   = new("Saves", "Relief appearances that preserve a lead per save rules."),
                ["SVO"]  = new("Save Opportunities", "Total chances to earn a save."),
                ["IP"]   = new("Innings Pitched", "Each out equals one third of an inning."),
                ["H"]    = new("Hits Allowed", "Total hits conceded to opponents."),
                ["R"]    = new("Runs Allowed", "Total runs (earned + unearned) conceded."),
                ["HR"]   = new("Home Runs Allowed", "Home runs conceded to opponents."),
                ["W"]    = new("Wins", "Games credited as wins."),
                ["SO"]   = new("Strikeouts", "Batters retired via strike three."),
                ["WHIP"] = new("Walks + Hits per Inning Pitched", "(BB + H) / IP."),
                ["AVG"]  = new("Batting Average Against", "Hits allowed / at-bats against."),
                ["TBF"]  = new("Total Batters Faced", "Total plate appearances against."),
                ["NP"]   = new("Number of Pitches", "Total balls + strikes thrown."),
                ["P/IP"] = new("Pitches per Inning", "Average pitches thrown per inning."),
                ["GF"]   = new("Games Finished", "Outings where the pitcher recorded the final out."),
                ["HLD"]  = new("Holds", "Relief outings preserving a lead without finishing the game."),
                ["IBB"]  = new("Intentional Walks", "Intentional bases on balls."),
                ["WP"]   = new("Wild Pitches", "Errant pitches allowing runners to advance."),
                ["K/BB"] = new("Strikeout-to-Walk Ratio", "Strikeouts divided by walks."),
                ["OP/G"] = new("Opponent Runs per Game", "Average runs allowed per game."),
                ["ER/G"] = new("Earned Runs per Game", "Average earned runs allowed per game."),
                ["SO/9"] = new("Strikeouts per 9", "(SO × 9) / IP."),
                ["H/9"]  = new("Hits per 9", "(H × 9) / IP."),
                ["HR/9"] = new("Home Runs per 9", "(HR × 9) / IP."),
                ["W/9"]  = new("Walks per 9", "(BB × 9) / IP.")
            },

            [StatDomain.TeamFielding] = new Dictionary<string, StatText>(StringComparer.OrdinalIgnoreCase)
            {
                ["DP"] = new("Double plays per game", "Average number of defensive double plays."),
                ["E"]  = new("Errors per game", "Average number of defensive errors.")
            },

            [StatDomain.CuriousFacts] = new Dictionary<string, StatText>(StringComparer.OrdinalIgnoreCase)
            {
                // YRFI/NRFI rates
                ["YRFI"]   = new("Yes Run First Inning %", "Share of games with a run scored in the 1st inning."),
                ["NRFI"]   = new("No Run First Inning %", "Share of games with zero runs in the 1st inning."),
                // Inning splits (team perspective; opponent variants handled in UI layer)
                ["1IR/G"]  = new("1st-inning runs per game", "Average runs scored in the 1st inning."),
                ["2IR/G"]  = new("2nd-inning runs per game", "Average runs scored in the 2nd inning."),
                ["3IR/G"]  = new("3rd-inning runs per game", "Average runs scored in the 3rd inning."),
                ["4IR/G"]  = new("4th-inning runs per game", "Average runs scored in the 4th inning."),
                ["5IR/G"]  = new("5th-inning runs per game", "Average runs scored in the 5th inning."),
                ["6IR/G"]  = new("6th-inning runs per game", "Average runs scored in the 6th inning."),
                ["7IR/G"]  = new("7th-inning runs per game", "Average runs scored in the 7th inning."),
                ["8IR/G"]  = new("8th-inning runs per game", "Average runs scored in the 8th inning."),
                ["9IR/G"]  = new("9th-inning runs per game", "Average runs scored in the 9th inning."),
                ["XTRAIR/G"] = new("Extra-innings runs per game", "Average runs scored in extra innings."),
                // Aggregates over first/last N innings
                ["F4IR/G"] = new("First 4 innings runs per game", "Average runs across innings 1–4."),
                ["F5IR/G"] = new("First 5 innings runs per game", "Average runs across innings 1–5."),
                ["F6IR/G"] = new("First 6 innings runs per game", "Average runs across innings 1–6."),
                ["L2IR/G"] = new("Last 2 innings runs per game", "Average runs across the final two regulation innings."),
                ["L3IR/G"] = new("Last 3 innings runs per game", "Average runs across the final three regulation innings."),
                ["L4IR/G"] = new("Last 4 innings runs per game", "Average runs across the final four regulation innings.")
            },

            [StatDomain.PlayerHitting] = new Dictionary<string, StatText>(StringComparer.OrdinalIgnoreCase)
            {
                ["Status"] = new("Player Status", "A => Active; M => Reassigned to Minors; D[n] => Days Injured."),
                ["G"]      = new("Games", "Number of games in which the player appeared."),
                ["AB"]     = new("At Bats", "Official at-bats excluding walks, HBP, sacrifices, interference."),
                ["R"]      = new("Runs", "Total runs scored."),
                ["H"]      = new("Hits", "Safe hits excluding errors or fielder's choice."),
                ["2B"]     = new("Doubles", "Hits reaching second base."),
                ["3B"]     = new("Triples", "Hits reaching third base."),
                ["HR"]     = new("Home Runs", "Hits where the batter circles all bases in one play."),
                ["RBI"]    = new("Runs Batted In", "Runs scored as a result of the player's at-bat (excluding errors)."),
                ["BB"]     = new("Walks", "Bases on balls."),
                ["SO"]     = new("Strikeouts", "Outs on strike three."),
                ["SB"]     = new("Stolen Bases", "Bases stolen without help of a hit/error."),
                ["CS"]     = new("Caught Stealing", "Times caught attempting to steal."),
                ["AVG"]    = new("Batting Average", "H/AB."),
                ["OBP"]    = new("On-base Percentage", "(H + BB + HBP) / (AB + BB + HBP + SF)."),
                ["SLG"]    = new("Slugging Percentage", "Total bases per at-bat."),
                ["OPS"]    = new("On-base Plus Slugging", "OBP + SLG."),
                ["PA"]     = new("Plate Appearances", "Completed batting appearances."),
                ["HBP"]    = new("Hit By Pitch", "Times awarded first base after being hit."),
                ["SAC"]    = new("Sacrifice Bunts", "Bunts that advance a runner while recording an out."),
                ["SF"]     = new("Sacrifice Flies", "Caught fly balls that score a runner."),
                ["GIDP"]   = new("Grounded Into Double Play", "Ground balls resulting in a double play."),
                ["GO/AO"]  = new("Groundouts to Airouts Ratio", "Groundout-to-flyout ratio."),
                ["XBH"]    = new("Extra-Base Hits", "Doubles + triples + home runs."),
                ["TB"]     = new("Total Bases", "1B×1 + 2B×2 + 3B×3 + HR×4."),
                ["IBB"]    = new("Intentional Walks", "Walks issued intentionally."),
                ["BABIP"]  = new("Batting Average on Balls in Play", "(H − HR) / (AB − SO − HR + SF)."),
                ["ISO"]    = new("Isolated Power", "SLG − AVG."),
                ["AB/HR"]  = new("At-bats per Home Run", "AB/HR."),
                ["BB/K"]   = new("Walk-to-Strikeout Ratio", "BB/SO."),
                ["BB%"]    = new("Walk Rate", "BB / PA."),
                ["SO%"]    = new("Strikeout Rate", "SO / PA."),
                ["HR%"]    = new("Home Run Rate", "HR / PA.")
            },

            [StatDomain.PlayerPitching] = new Dictionary<string, StatText>(StringComparer.OrdinalIgnoreCase)
            {
                ["Status"] = new("Player Status", "A => Active; RM => Reassigned to Minors; D[n] => Days Injured."),
                // Basic
                ["W"]     = new("Wins", "Games credited as wins."),
                ["L"]     = new("Losses", "Games credited as losses."),
                ["ERA"]   = new("Earned Run Average", "(ER × 9) / IP."),
                ["G"]     = new("Games", "Pitching appearances."),
                ["GS"]    = new("Games Started", "Starts by the pitcher."),
                ["CG"]    = new("Complete Games", "Starts completed without relief."),
                ["SHO"]   = new("Shutouts", "Complete games with zero runs allowed."),
                ["SV"]    = new("Saves", "Relief appearances preserving a lead."),
                ["SVO"]   = new("Save Opportunities", "Total chances to earn a save."),
                ["IP"]    = new("Innings Pitched", "Each out equals one third of an inning."),
                ["R"]     = new("Runs Allowed", "Total runs conceded."),
                ["H"]     = new("Hits Allowed", "Hits conceded to opponents."),
                ["ER"]    = new("Earned Runs", "Runs counted as earned."),
                ["HR"]    = new("Home Runs Allowed", "Home runs conceded."),
                ["HB"]    = new("Hit Batsmen", "Batters hit by pitch."),
                ["BB"]    = new("Walks", "Bases on balls issued."),
                ["SO"]    = new("Strikeouts", "Batters retired via strike three."),
                ["WHIP"]  = new("Walks + Hits per Inning Pitched", "(BB + H) / IP."),
                ["AVG"]   = new("Batting Average Against", "H / AB against."),
                // Advanced
                ["TBF"]   = new("Total Batters Faced", "Plate appearances against."),
                ["NP"]    = new("Number of Pitches", "Total pitches thrown."),
                ["P/IP"]  = new("Pitches per Inning", "Average pitches per inning."),
                ["QS"]    = new("Quality Starts", "≥6 IP and ≤3 ER."),
                ["GF"]    = new("Games Finished", "Outings recording the final out."),
                ["HLD"]   = new("Holds", "Relief outings preserving a lead without finishing."),
                ["IBB"]   = new("Intentional Walks", "Intentional bases on balls."),
                ["WP"]    = new("Wild Pitches", "Errant pitches allowing advances."),
                ["BK"]    = new("Balks", "Illegal pitching motions/actions."),
                ["GDP"]   = new("Grounded into Double Play", "Opposing ground balls resulting in a double play."),
                ["GO/AO"] = new("Groundouts to Airouts Ratio", "Induced groundout-to-flyout ratio."),
                ["SO/9"]  = new("Strikeouts per 9", "(SO × 9) / IP."),
                ["BB/9"]  = new("Walks per 9", "(BB × 9) / IP."),
                ["H/9"]   = new("Hits per 9", "(H × 9) / IP."),
                ["K/BB"]  = new("Strikeout-to-Walk Ratio", "SO / BB."),
                ["BABIP"] = new("Batting Average on Balls in Play", "(H − HR) / (AB − SO − HR + SF)."),
                ["SB"]    = new("Stolen Bases Allowed", "Stolen bases allowed with this pitcher."),
                ["CS"]    = new("Caught Stealing", "Runners caught stealing with this pitcher."),
                ["PK"]    = new("Pickoffs", "Baserunners picked off by the pitcher.")
            },

            [StatDomain.PlayerFielding] = new Dictionary<string, StatText>(StringComparer.OrdinalIgnoreCase)
            {
                ["OUTS"] = new("Outs", "Total defensive outs recorded."),
                ["TC"]   = new("Total Chances", "Putouts + assists + errors."),
                ["CH"]   = new("Chances", "Opportunities to make a play (PO + A + E)."),
                ["PO"]   = new("Putouts", "Outs via tag, force, or caught fly ball."),
                ["A"]    = new("Assists", "Times the player assists on an out."),
                ["E"]    = new("Errors", "Defensive misplays allowing advancement."),
                ["DP"]   = new("Double Plays", "Double plays the player participated in."),
                ["PB"]   = new("Passed Balls", "Catchable pitches not handled by the catcher."),
                ["CASB"] = new("Stolen Bases Allowed", "Successful steals against."),
                ["CACS"] = new("Caught Stealing", "Runners thrown out attempting to steal."),
                ["FLD%"] = new("Fielding Percentage", "(PO + A) / (PO + A + E).")
            },
        };

        /// <summary>
        /// Tries to resolve a stat description for a given domain/abbreviation.
        /// </summary>
        public static bool TryGet(StatDomain domain, string abbr, out StatText text)
        {
            text = default!;
            if (!_maps.TryGetValue(domain, out var map)) return false;
            return map.TryGetValue(abbr, out text);
        }

        /// <summary>
        /// Returns the full map for a domain. Empty map when domain has no entries.
        /// </summary>
        public static IReadOnlyDictionary<string, StatText> GetMap(StatDomain domain)
            => _maps.TryGetValue(domain, out var map) ? map : new Dictionary<string, StatText>();
    }
}
