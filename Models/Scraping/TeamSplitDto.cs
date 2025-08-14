namespace StrikeData.Models.Scraping;

public sealed class TeamSplitDto
{
    public string Opponent { get; init; } = string.Empty;
    public int Games { get; init; }
    public int Won { get; init; }
    public int Lost { get; init; }
    public decimal WinPercentage { get; init; }
}
