using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Application.Interfaces;

public interface IReadingRepository
{
    public Task AddAsync(ReadingRecord record, CancellationToken cancellationToken = default);
    public Task AddRangeAsync(IEnumerable<ReadingRecord> records, CancellationToken cancellationToken = default);
    public Task<bool> UpdateAsync(ReadingRecord record, CancellationToken cancellationToken = default);
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    public Task<int> DeleteByDatesAsync(IReadOnlyList<DateOnly> dates, CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<ReadingRecord>> GetAllAsync(CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<ReadingRecord>> GetByDateRangeAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default);
    public Task<int> GetReadCountAsync(int psalmId, CancellationToken cancellationToken = default);
    public Task ClearRangeAsync(DateOnly start, DateOnly end, DateOnly? minDateInclusive = null, CancellationToken cancellationToken = default);
}
