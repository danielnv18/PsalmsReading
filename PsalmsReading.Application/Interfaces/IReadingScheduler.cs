using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Application.Interfaces;

public interface IReadingScheduler
{
    Task<IReadOnlyList<PlannedReading>> GenerateScheduleAsync(DateOnly startDate, int months, CancellationToken cancellationToken = default);
}
