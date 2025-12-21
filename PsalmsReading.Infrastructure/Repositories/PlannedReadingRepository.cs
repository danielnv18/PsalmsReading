using Microsoft.EntityFrameworkCore;
using PsalmsReading.Application.Interfaces;
using PsalmsReading.Domain.Entities;
using PsalmsReading.Infrastructure.Data;

namespace PsalmsReading.Infrastructure.Repositories;

public sealed class PlannedReadingRepository : IPlannedReadingRepository
{
    private readonly PsalmsDbContext _dbContext;

    public PlannedReadingRepository(PsalmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SavePlansAsync(IEnumerable<PlannedReading> plans, CancellationToken cancellationToken = default)
    {
        var list = plans.ToList();
        if (list.Count == 0)
        {
            return;
        }

        _dbContext.PlannedReadings.AddRange(list);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlannedReading>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.PlannedReadings
            .AsNoTracking()
            .OrderBy(p => p.ScheduledDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlannedReading>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PlannedReadings
            .AsNoTracking()
            .Where(p => p.ScheduledDate >= from && p.ScheduledDate <= to)
            .OrderBy(p => p.ScheduledDate)
            .ToListAsync(cancellationToken);
    }

    public async Task ClearRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        List<PlannedReading> existing = await _dbContext.PlannedReadings
            .Where(p => p.ScheduledDate >= from && p.ScheduledDate <= to)
            .ToListAsync(cancellationToken);

        if (existing.Count == 0)
        {
            return;
        }

        _dbContext.PlannedReadings.RemoveRange(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
