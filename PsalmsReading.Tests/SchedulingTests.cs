using PsalmsReading.Application.Interfaces;
using PsalmsReading.Domain.Entities;
using PsalmsReading.Infrastructure.Services;

namespace PsalmsReading.Tests;

public class SchedulingTests
{
    [Fact]
    public async Task Picks_Alabanza_On_First_Sunday()
    {
        var psalms = new List<Psalm>
        {
            new(1, "General", 20, null, null, new List<string>()),
            new(2, "Alabanza Psalm", 20, "alabanza", null, new List<string>())
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository());

        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 1, 1), 1);

        Assert.Contains(plans, p => p.PsalmId == 2);
    }

    [Fact]
    public async Task Picks_HolyWeek_Psalms_With_Fewer_Reads()
    {
        var psalms = new List<Psalm>
        {
            new(113, "Psalm 113", 20, null, null, new List<string>()),
            new(114, "Psalm 114", 20, null, null, new List<string>())
        };

        var pastReads = new List<ReadingRecord>
        {
            new(Guid.NewGuid(), 114, new DateOnly(2024, 1, 7))
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(pastReads));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 4, 1), 1);

        Assert.Contains(plans, p => p.PsalmId == 113);
    }

    [Fact]
    public async Task December_Falls_Back_To_Theme_When_Type_Missing()
    {
        var psalms = new List<Psalm>
        {
            new(10, "General", 20, null, null, new List<string>()),
            new(11, "Mesiánico", 20, null, null, new List<string> { "mesiánico" })
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository());
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 12, 1), 1);

        Assert.Contains(plans, p => p.PsalmId == 11);
    }

    private sealed class FakePsalmRepository : IPsalmRepository
    {
        private readonly IReadOnlyList<Psalm> _psalms;

        public FakePsalmRepository(IReadOnlyList<Psalm> psalms)
        {
            _psalms = psalms;
        }

        public Task<IReadOnlyList<Psalm>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult(_psalms);

        public Task<Psalm?> GetByIdAsync(int psalmId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_psalms.FirstOrDefault(p => p.Id == psalmId));

        public Task AddRangeAsync(IEnumerable<Psalm> psalms, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ReplaceAllAsync(IEnumerable<Psalm> psalms, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> AnyAsync(CancellationToken cancellationToken = default) => Task.FromResult(_psalms.Any());
    }

    private sealed class FakeReadingRepository : IReadingRepository
    {
        private readonly List<ReadingRecord> _records;

        public FakeReadingRepository(IEnumerable<ReadingRecord>? records = null)
        {
            _records = records?.ToList() ?? new List<ReadingRecord>();
        }

        public Task AddAsync(ReadingRecord record, CancellationToken cancellationToken = default)
        {
            _records.Add(record);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ReadingRecord>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ReadingRecord>>(_records);

        public Task<IReadOnlyList<ReadingRecord>> GetByDateRangeAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
        {
            var result = _records.Where(r => r.DateRead >= start && r.DateRead <= end).ToList();
            return Task.FromResult<IReadOnlyList<ReadingRecord>>(result);
        }

        public Task<int> GetReadCountAsync(int psalmId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_records.Count(r => r.PsalmId == psalmId));
    }
}
