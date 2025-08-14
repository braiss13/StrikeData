namespace StrikeData.Models.Scraping;

public sealed class TeamScheduleResultDto
{
    public List<ScheduleEntryDto> Schedule { get; } = new();
    public List<MonthlySplitDto> MonthlySplits { get; } = new();
    public List<TeamSplitDto> TeamSplits { get; } = new();
}
