namespace StrikeData.Models.Scraping;

public sealed class ScheduleEntryDto
{
    public int GameNumber { get; init; }
    public DateTime Date { get; init; }
    public string Opponent { get; init; } = string.Empty;
    public string Score { get; init; } = string.Empty;
    public string Decision { get; init; } = string.Empty;
    public string Record { get; init; } = string.Empty;
}
