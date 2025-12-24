namespace PsalmsReading.Application.Models;

public enum ReadingExportRange
{
    All,
    Year
}

public enum ReadingImportMode
{
    ReplaceConflicts,
    IgnoreConflicts
}

public sealed record ReadingExportDto(
    int SchemaVersion,
    DateTimeOffset ExportedAt,
    string Range,
    int? Year,
    IReadOnlyList<ReadingExportRecordDto> Records);

public sealed record ReadingExportRecordDto(Guid Id, int PsalmId, DateOnly DateRead, string? RuleApplied);

public sealed record ReadingImportPreviewDto(
    int TotalRecords,
    int ConflictCount,
    IReadOnlyList<DateOnly> ConflictDates);

public sealed record ReadingImportResultDto(
    int ImportedCount,
    int SkippedCount,
    int ReplacedDates);
