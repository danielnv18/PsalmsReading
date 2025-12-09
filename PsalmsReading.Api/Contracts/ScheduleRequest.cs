namespace PsalmsReading.Api.Contracts;

public sealed record ScheduleRequest(DateOnly StartDate, int Months);
