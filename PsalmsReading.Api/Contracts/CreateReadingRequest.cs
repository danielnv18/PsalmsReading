namespace PsalmsReading.Api.Contracts;

public sealed record CreateReadingRequest(int PsalmId, DateOnly DateRead);
