namespace PsalmsReading.Api.Contracts;

public sealed record BulkDeleteReadingsRequest(IReadOnlyList<DateOnly> Dates);
