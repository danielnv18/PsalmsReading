namespace PsalmsReading.Application.Models;

public sealed record ReadingRecordDto(Guid Id, int PsalmId, DateOnly DateRead, string? RuleApplied);
