namespace PsalmsReading.UI.Models;

public record PsalmDto(int Id, string Title, int TotalVerses, string? Type, IReadOnlyList<string> Epigraphs, IReadOnlyList<string> Themes);

public record ReadingRecordDto(Guid Id, int PsalmId, DateOnly DateRead);

public record PlannedReadingDto(Guid Id, int PsalmId, DateOnly ScheduledDate, string RuleApplied);

public record CreateReadingRequest(int PsalmId, DateOnly DateRead);

public record ScheduleRequest(DateOnly StartDate, int Months);
