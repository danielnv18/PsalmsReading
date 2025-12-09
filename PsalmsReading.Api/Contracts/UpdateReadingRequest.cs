namespace PsalmsReading.Api.Contracts;

public sealed record UpdateReadingRequest(int PsalmId, DateOnly DateRead);
