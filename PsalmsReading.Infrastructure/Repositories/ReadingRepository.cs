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
}
