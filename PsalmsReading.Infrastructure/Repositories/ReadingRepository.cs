using Microsoft.EntityFrameworkCore;
using PsalmsReading.Application.Interfaces;
using PsalmsReading.Domain.Entities;
using PsalmsReading.Infrastructure.Data;

namespace PsalmsReading.Infrastructure.Repositories;

public sealed class ReadingRepository : IReadingRepository
{
    private readonly PsalmsDbContext _dbContext;

    public ReadingRepository(PsalmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ReadingRecord record, CancellationToken cancellationToken = default)
    {
        _dbContext.ReadingRecords.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<ReadingRecord> records, CancellationToken cancellationToken = default)
    {
        List<ReadingRecord> list = records.ToList();
        if (list.Count == 0)
        {
            return;
        }

        _dbContext.ReadingRecords.AddRange(list);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(ReadingRecord record, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.ReadingRecords.AnyAsync(r => r.Id == record.Id, cancellationToken);
        if (!exists)
        {
            return false;
        }

        _dbContext.ReadingRecords.Update(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.ReadingRecords.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        _dbContext.ReadingRecords.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> DeleteByDatesAsync(IReadOnlyList<DateOnly> dates, CancellationToken cancellationToken = default)
    {
        if (dates.Count == 0)
        {
            return 0;
        }

        List<DateOnly> distinctDates = dates.Distinct().ToList();
        List<ReadingRecord> existing = await _dbContext.ReadingRecords
            .Where(r => distinctDates.Contains(r.DateRead))
            .ToListAsync(cancellationToken);

        if (existing.Count == 0)
        {
            return 0;
        }

        _dbContext.ReadingRecords.RemoveRange(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing.Count;
    }

    public async Task<IReadOnlyList<ReadingRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ReadingRecords.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReadingRecord>> GetByDateRangeAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ReadingRecords
            .AsNoTracking()
            .Where(r => r.DateRead >= start && r.DateRead <= end)
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetReadCountAsync(int psalmId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ReadingRecords.AsNoTracking().CountAsync(r => r.PsalmId == psalmId, cancellationToken);
    }

    public async Task ClearRangeAsync(DateOnly start, DateOnly end, DateOnly? minDateInclusive = default, CancellationToken cancellationToken = default)
    {
        IQueryable<ReadingRecord> query = _dbContext.ReadingRecords
            .Where(r => r.DateRead >= start && r.DateRead <= end);

        if (minDateInclusive.HasValue)
        {
            DateOnly minDate = minDateInclusive.Value;
            query = query.Where(r => r.DateRead >= minDate);
        }

        List<ReadingRecord> existing = await query.ToListAsync(cancellationToken);
        if (existing.Count == 0)
        {
            return;
        }

        _dbContext.ReadingRecords.RemoveRange(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
