using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Application.Interfaces;

public interface IReadingRepository
{
    Task AddAsync(ReadingRecord record, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(ReadingRecord record, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReadingRecord>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReadingRecord>> GetByDateRangeAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default);
    Task<int> GetReadCountAsync(int psalmId, CancellationToken cancellationToken = default);
}
