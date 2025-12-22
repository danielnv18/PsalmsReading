namespace PsalmsReading.Application.Models;

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
