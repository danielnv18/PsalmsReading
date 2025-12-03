namespace PsalmsReading.Application.Models;

public sealed record PlannedReadingDto(Guid Id, int PsalmId, DateOnly ScheduledDate, string RuleApplied);
