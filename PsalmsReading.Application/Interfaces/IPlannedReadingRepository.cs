using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Application.Interfaces;

public interface IPlannedReadingRepository
{
    public Task SavePlansAsync(IEnumerable<PlannedReading> plans, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlannedReading>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlannedReading>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    public Task ClearRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
