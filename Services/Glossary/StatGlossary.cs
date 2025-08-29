namespace StrikeData.Services.Glossary
{
    // Dominios para desambiguar abreviaturas repetidas
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

    public record StatText(string LongName, string Description);

    public static class StatGlossary
    {
        // Mapas por dominio. Usa StringComparer.OrdinalIgnoreCase para siglas.
        private static readonly IReadOnlyDictionary<StatDomain, IReadOnlyDictionary<string, StatText>> _maps = new Dictionary<StatDomain, IReadOnlyDictionary<string, StatText>>
        {

            [StatDomain.TeamHitting]   = new Dictionary<string, StatText>(StringComparer.OrdinalIgnoreCase) { 
                ["AVG"]   = new("Batting average", "Hits divided by at-bats (H/AB)"),
                ["OBP"]   = new("On-base percentage", "(H + BB + HBP) / (AB + BB + HBP + SF)"),
                ["SLG"]   = new("Slugging percentage", "Total bases per at-bat"),
                ["OPS"]   = new("On-base plus slugging", "OBP + SLG"),
                ["R"]     = new("Runs scored", "Number of times a player safely reaches home plate."),
                ["H"]     = new("Hits", "Number of times a batter reaches base safely on a fair ball without an error or fielders choice"),
                ["HR"]    = new("Home runs", "Hits where the batter circles all bases in one play, typically over the fence"),
                ["RBI"]   = new("Runs batted in", "Runs scored as a result of the batters at-bat (excluding errors)"),
                ["BB"]    = new("Walks", "Plate appearances resulting in four balls"),
                ["SO"]    = new("Strikeouts", "Outs recorded on strike three"),
                ["SB"]    = new("Stolen bases", "Bases taken successfully without the help of a hit or error"),
                ["CS"]    = new("Caught stealing", "Runners thrown out while attempting to steal"),
                ["2B"]    = new("Doubles", "Hits on which the batter reaches second base"),
                ["3B"]    = new("Triples", "Hits on which the batter reaches third base"),
                ["AB"]    = new("At-bats", "Plate appearances excluding walks, hit by pitch, sacrifices, and interference"),
                ["AB/HR"] = new("At-bats per home run", "AB divided by HR"),
                ["GIDP"]  = new("Ground into double play", "When a player hits a ground ball that results in more than one out on the bases"),
                ["HBP"]   = new("Hit by pitch", "When a batter is struck by a pitched ball without swinging"),
                ["LOB"]   = new("Left on base", "Number of runners left on base at the end of an inning"),
                ["RLSP"]  = new("Runners left in scoring position", "Average number of runners who finish an inning on second or third base, without having scored, per game a team plays"),
                ["S"]     = new("Singles", "Hits on which the batter reaches first base"),
                ["SAC"]   = new("Sacrifice hits", "Bunts that advance a runner while resulting in an out"),
                ["SBA"]   = new("Stolen base attempts", "Total number of times a player tries to steal a base, both successful and caught"),
                ["SF"]    = new("Sacrifice flies", "Fly balls that allow a runner to score after the catch"),
                ["TB"]    = new("Total bases", "Cumulative number of bases a player earns from hits (1 for single, 2 for double, etc.)"),
                ["TLOB"]  = new("Total left on base", "Total number of runners left on base at the end of each half-inning")

            },

            [StatDomain.TeamPitching] = new Dictionary<string, StatText>(StringComparer.OrdinalIgnoreCase)
            {
                ["ERA"]  = new("Earned Run Average", "Earned runs allowed per nine innings pitched ((earned runs × 9) / innings pitched)."),
                ["SHO"]  = new("Shutouts", "Complete games where no runs are allowed by the pitcher or team."),
                ["CG"]   = new("Complete Games", "Games in which the starting pitcher pitches the entire game without relief."),
                ["SV"]   = new("Saves", "Relief appearances that preserve a lead while meeting the save criteria."),
                ["SVO"]  = new("Save Opportunities", "Total chances a pitcher has to earn a save (regardless of outcome)."),
                ["IP"]   = new("Innings Pitched", "Total innings thrown; each out equals one third of an inning."),
                ["H"]    = new("Hits Allowed", "Total hits conceded to opposing batters. A hit occurs when a batter reaches at least first base safely after putting the ball in play, without an error or fielder's choice."),
                ["R"]    = new("Runs Allowed", "Total runs (earned and unearned) given up by the pitcher or team. A run scores when a runner safely circles the bases and touches home plate."),
                ["HR"]   = new("Home Runs Allowed", "Number of home runs conceded to opponents. A home run occurs when a batted ball in fair territory clears the outfield fence or the batter circles all the bases on an inside-the-park hit."),
                ["W"]    = new("Wins", "Games credited as wins to the pitcher or team."),
                ["SO"]   = new("Strikeouts", "Number of batters retired via strike three."),
                ["WHIP"] = new("Walks + Hits per Inning Pitched", "(Walks + Hits) divided by innings pitched; measures baserunners allowed."),
                ["AVG"]  = new("Batting Average Against", "Opponents' batting average; hits allowed divided by at-bats against."),
                // Advanced
                ["TBF"]  = new("Total Batters Faced", "Number of plate appearances against the pitcher or team."),
                ["NP"]   = new("Number of Pitches", "Total pitches thrown (balls and strikes)."),
                ["P/IP"] = new("Pitches per Inning", "Average number of pitches thrown per inning pitched."),
                ["GF"]   = new("Games Finished", "Appearances where the pitcher recorded the final out for his team."),
                ["HLD"]  = new("Holds", "Relief outings where the pitcher enters in a save situation, records at least one out and leaves with the lead intact."),
                ["IBB"]  = new("Intentional Walks", "Walks issued intentionally by the pitcher."),
                ["WP"]   = new("Wild Pitches", "Errant pitches that allow baserunners to advance."),
                ["K/BB"] = new("Strikeout-to-Walk Ratio", "Strikeouts divided by walks."),
                ["OP/G"] = new("Opponent Runs per Game", "Average runs allowed per game."),
                ["ER/G"] = new("Earned Runs per Game", "Average earned runs allowed per game."),
                ["SO/9"] = new("Strikeouts per 9", "(Strikeouts × 9) / innings pitched."),
                ["H/9"]  = new("Hits per 9", "(Hits allowed × 9) / innings pitched."),
                ["HR/9"] = new("Home Runs per 9", "(Home runs allowed × 9) / innings pitched."),
                ["W/9"]  = new("Walks per 9", "(Walks × 9) / innings pitched.")

            },

            [StatDomain.TeamFielding]  = new Dictionary<string, StatText>(StringComparer.OrdinalIgnoreCase)
            { 
                ["DP"] = new("Double plays per game", "Average number of defensive plays that record two outs in one continuous sequence."),
                ["E"]  = new("Errors per game", "Average number of misplays that allow a runner to reach or advance when an ordinary effort should have produced an out.")
            },

            [StatDomain.CuriousFacts]  = new Dictionary<string, StatText>(StringComparer.OrdinalIgnoreCase)
            {
                // Porcentajes Y/N Run 1st Inning
                ["YRFI"]   = new("Yes Run First Inning %", "Share of games with at least one run scored in the 1st inning."),
                ["NRFI"]   = new("No Run First Inning %", "Share of games with zero runs scored in the 1st inning."),
                // Inning-specific runs per game (team base key; la perspectiva se indica en el UI)
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
                // First N innings aggregates
                ["F4IR/G"] = new("First 4 innings runs per game", "Average runs scored across innings 1–4."),
                ["F5IR/G"] = new("First 5 innings runs per game", "Average runs scored across innings 1–5."),
                ["F6IR/G"] = new("First 6 innings runs per game", "Average runs scored across innings 1–6."),
                // Last N innings aggregates (regulation)
                ["L2IR/G"] = new("Last 2 innings runs per game", "Average runs scored across the final two regulation innings (typically 8–9)."),
                ["L3IR/G"] = new("Last 3 innings runs per game", "Average runs scored across the final three regulation innings (typically 7–9)."),
                ["L4IR/G"] = new("Last 4 innings runs per game", "Average runs scored across the final four regulation innings (typically 6–9).")

            },

            [StatDomain.PlayerHitting] = new Dictionary<string, StatText>(StringComparer.OrdinalIgnoreCase)
            {
                ["Status"] = new("Player Status", "A => Active; M => Reassigned to Minors; D[n] => Days Injured (n = number of days)."),
                ["G"]      = new("Games", "Number of games in which the player appeared."),
                ["AB"]     = new("At Bats", "Official at-bats, excluding walks, hit-by-pitch, sacrifices and interference."),
                ["R"]      = new("Runs", "Total runs scored by the player."),
                ["H"]      = new("Hits", "Number of times the player reaches at least first base safely on a fair ball without an error or fielder's choice."),
                ["2B"]     = new("Doubles", "Hits on which the batter safely reaches second base."),
                ["3B"]     = new("Triples", "Hits on which the batter safely reaches third base."),
                ["HR"]     = new("Home Runs", "Hits on which the batter circles all bases in one play, usually by hitting the ball over the outfield fence."),
                ["RBI"]    = new("Runs Batted In", "Runs scored as a result of the player's at-bat, except when due to errors."),
                ["BB"]     = new("Walks", "Number of times the player reaches first base after four balls."),
                ["SO"]     = new("Strikeouts", "Number of times the player is retired by strike three."),
                ["SB"]     = new("Stolen Bases", "Number of bases stolen by the player without the help of a hit or error."),
                ["CS"]     = new("Caught Stealing", "Times the player is thrown out while attempting to steal a base."),
                ["AVG"]    = new("Batting Average", "Hits divided by at-bats: H/AB."),
                ["OBP"]    = new("On-base Percentage", "Frequency the player reaches base safely: (H + BB + HBP) / (AB + BB + HBP + SF)."),
                ["SLG"]    = new("Slugging Percentage", "Total bases per at-bat: (1×1B + 2×2B + 3×3B + 4×HR) / AB."),
                ["OPS"]    = new("On-base Plus Slugging", "Sum of on-base percentage and slugging percentage (OBP + SLG)."),
                // Advanced hitting stats definitions
                ["PA"]     = new("Plate Appearances", "Total completed batting appearances, including at-bats, walks, hit-by-pitch, sacrifices and times reached by interference."),
                ["HBP"]    = new("Hit By Pitch", "Number of times the batter is awarded first base after being hit by a pitched ball."),
                ["SAC"]    = new("Sacrifice Bunts", "Number of bunts that advance a runner while the batter is thrown out."),
                ["SF"]     = new("Sacrifice Flies", "Number of fly balls caught for outs that allow a runner to score."),
                ["GIDP"]   = new("Grounded Into Double Play", "Number of ground balls that result in a double play."),
                ["GO/AO"]  = new("Groundouts to Airouts Ratio", "Ratio of outs on ground balls to outs on fly balls."),
                ["XBH"]    = new("Extra-Base Hits", "Total number of doubles, triples and home runs."),
                ["TB"]     = new("Total Bases", "Sum of bases gained by hits: singles (1), doubles (2), triples (3) and home runs (4)."),
                ["IBB"]    = new("Intentional Walks", "Number of times the player is walked intentionally by the opposing team."),
                ["BABIP"]  = new("Batting Average on Balls in Play", "Average on balls put in play excluding home runs: (H − HR) / (AB − SO − HR + SF)."),
                ["ISO"]    = new("Isolated Power", "Power metric calculated as slugging percentage minus batting average (SLG − AVG)."),
                ["AB/HR"]  = new("At-bats per Home Run", "Average number of at-bats between home runs: AB/HR."),
                ["BB/K"]   = new("Walk-to-Strikeout Ratio", "Walks divided by strikeouts: BB/SO."),
                ["BB%"]    = new("Walk Rate", "Percentage of plate appearances resulting in walks: BB/PA."),
                ["SO%"]    = new("Strikeout Rate", "Percentage of plate appearances resulting in strikeouts: SO/PA."),
                ["HR%"]    = new("Home Run Rate", "Percentage of plate appearances resulting in home runs: HR/PA.")

            },

            [StatDomain.PlayerPitching]= new Dictionary<string, StatText>(StringComparer.OrdinalIgnoreCase)
            { 
                ["Status"] = new("Player Status", "A => Active; RM => Reassigned to Minors; D[n] => Days Injured (n = number of days)."),
                // Basic statistics definitions
                ["W"]     = new("Wins", "Number of games credited as wins to the pitcher or team."),
                ["L"]     = new("Losses", "Number of games credited as losses to the pitcher or team."),
                ["ERA"]   = new("Earned Run Average", "Earned runs allowed per nine innings pitched ((earned runs × 9) / innings pitched)."),
                ["G"]     = new("Games", "Number of games in which the pitcher appeared."),
                ["GS"]    = new("Games Started", "Number of games started by the pitcher."),
                ["CG"]    = new("Complete Games", "Number of games in which the pitcher threw the entire game without relief."),
                ["SHO"]   = new("Shutouts", "Complete games where the pitcher allowed no runs."),
                ["SV"]    = new("Saves", "Relief appearances that preserve a lead while meeting save criteria."),
                ["SVO"]   = new("Save Opportunities", "Total opportunities the pitcher has to earn a save, regardless of outcome."),
                ["IP"]    = new("Innings Pitched", "Total innings thrown; each out counts as one third of an inning."),
                ["R"]     = new("Runs Allowed", "Total runs (earned and unearned) given up by the pitcher or team."),
                ["H"]     = new("Hits Allowed", "Number of hits conceded to opposing batters. A hit occurs when a batter reaches at least first base safely on a fair ball without an error or fielder's choice."),
                ["ER"]    = new("Earned Runs", "Number of earned runs allowed by the pitcher. Earned runs exclude those that score due to errors or passed balls."),
                ["HR"]    = new("Home Runs Allowed", "Number of home runs conceded. A home run occurs when a batted ball allows the batter to round all bases in one play."),
                ["HB"]    = new("Hit Batsmen", "Number of times the pitcher hits a batter with a pitched ball, awarding first base."),
                ["BB"]    = new("Walks", "Number of bases on balls issued: times the pitcher throws four balls, allowing the batter to walk to first base."),
                ["SO"]    = new("Strikeouts", "Number of batters retired via strike three."),
                ["WHIP"]  = new("Walks + Hits per Inning Pitched", "(walks + hits) divided by innings pitched; measures baserunners allowed."),
                ["AVG"]   = new("Batting Average Against", "Opponents' batting average: hits allowed divided by at-bats against."),
                // Advanced statistics definitions
                ["TBF"]   = new("Total Batters Faced", "Number of batters faced by the pitcher."),
                ["NP"]    = new("Number of Pitches", "Total pitches thrown, including balls and strikes."),
                ["P/IP"]  = new("Pitches per Inning", "Average number of pitches thrown per inning pitched."),
                ["QS"]    = new("Quality Starts", "Number of starts where the pitcher pitches at least six innings and allows three or fewer earned runs."),
                ["GF"]    = new("Games Finished", "Number of games in which the pitcher recorded the final out."),
                ["HLD"]   = new("Holds", "Relief outings where the pitcher enters in a save situation, records at least one out and leaves with the lead intact."),
                ["IBB"]   = new("Intentional Walks", "Number of intentional bases on balls issued by the pitcher."),
                ["WP"]    = new("Wild Pitches", "Number of pitches so errant that a baserunner advances and it is not scored as a passed ball."),
                ["BK"]    = new("Balks", "Number of illegal pitching motions or actions that allow baserunners to advance."),
                ["GDP"]   = new("Grounded into Double Play", "Number of opponent ground balls off the pitcher that result in a double play."),
                ["GO/AO"] = new("Groundouts to Airouts Ratio", "Ratio of outs on ground balls to outs on fly balls induced by the pitcher."),
                ["SO/9"]  = new("Strikeouts per 9", "(strikeouts × 9) / innings pitched."),
                ["BB/9"]  = new("Walks per 9", "(walks × 9) / innings pitched."),
                ["H/9"]   = new("Hits per 9", "(hits allowed × 9) / innings pitched."),
                ["K/BB"]  = new("Strikeout-to-Walk Ratio", "Strikeouts divided by walks (K/BB)."),
                ["BABIP"] = new("Batting Average on Balls in Play", "Average on balls put in play excluding home runs: (hits − home runs) / (at-bats − strikeouts − home runs + sacrifice flies)."),
                ["SB"]    = new("Stolen Bases Allowed", "Number of stolen bases allowed while the pitcher is on the mound."),
                ["CS"]    = new("Caught Stealing", "Number of baserunners caught stealing while the pitcher is on the mound."),
                ["PK"]    = new("Pickoffs", "Number of times the pitcher picks off a baserunner.")
            },

            [StatDomain.PlayerFielding] = new Dictionary<string, StatText>(StringComparer.OrdinalIgnoreCase)
            {
                ["OUTS"] = new("Outs", "Total defensive outs recorded by the player."),
                ["TC"]   = new("Total Chances", "Total defensive chances: putouts + assists + errors."),
                ["CH"]   = new("Chances", "Number of opportunities to make a play (putouts + assists + errors)."),
                ["PO"]   = new("Putouts", "Number of outs credited by tagging a runner, force plays or catching a fly ball."),
                ["A"]    = new("Assists", "Number of times the player assists on an out."),
                ["E"]    = new("Errors", "Defensive miscues allowing a runner to reach or advance."),
                ["DP"]   = new("Double Plays", "Number of double plays in which the player participated."),
                ["PB"]   = new("Passed Balls", "Number of pitches a catcher fails to handle, allowing runners to advance."),
                ["CASB"] = new("Stolen Bases Allowed", "Baserunners who successfully stole while the player was fielding."),
                ["CACS"] = new("Caught Stealing", "Baserunners thrown out while attempting to steal a base."),
                ["FLD%"] = new("Fielding Percentage", "Fielding percentage: (putouts + assists) divided by total chances.")
            },

        };

        public static bool TryGet(StatDomain domain, string abbr, out StatText text)
        {
            text = default!;
            if (!_maps.TryGetValue(domain, out var map)) return false;
            return map.TryGetValue(abbr, out text);
        }

        public static IReadOnlyDictionary<string, StatText> GetMap(StatDomain domain)
            => _maps.TryGetValue(domain, out var map) ? map : new Dictionary<string, StatText>();
    }
}
