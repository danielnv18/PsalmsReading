using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Application.Interfaces;

public interface IPlannedReadingRepository
{
    Task SavePlansAsync(IEnumerable<PlannedReading> plans, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlannedReading>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task ClearRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
