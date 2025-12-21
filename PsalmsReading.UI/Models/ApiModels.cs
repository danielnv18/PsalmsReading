namespace PsalmsReading.UI.Models;

public record PsalmDto(int Id, string Title, int TotalVerses, string? Type, IReadOnlyList<string> Epigraphs, IReadOnlyList<string> Themes);

public record ReadingRecordDto(Guid Id, int PsalmId, DateOnly DateRead);

public record PlannedReadingDto(Guid Id, int PsalmId, DateOnly ScheduledDate, string RuleApplied);

public record CreateReadingRequest(int PsalmId, DateOnly DateRead);

public record UpdateReadingRequest(int PsalmId, DateOnly DateRead);

public record ScheduleRequest(DateOnly StartDate, int Months);

public sealed record StatsDto(
    string Range,
    DateOnly? RangeStart,
    DateOnly? RangeEnd,
    int TotalReadablePsalms,
    int ReadablePsalmsRead,
    int ReadablePsalmsProjected,
    int ActualReadsInRange,
    int PlannedReadsInRange,
    IReadOnlyList<TypeStatsDto> Types);

public sealed record TypeStatsDto(
    string Type,
    int TotalReadablePsalms,
    int ActualReadsInRange,
    int PlannedReadsInRange,
    int ActualCoverageCount,
    int ProjectedCoverageCount);
