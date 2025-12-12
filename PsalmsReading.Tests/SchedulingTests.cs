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

    [Fact]
    public async Task Excludes_Long_And_Banned_Psalms()
    {
        var psalms = new List<Psalm>
        {
            new(35, "Excluded", 20, null, null, new List<string>()), // banned id
            new(120, "Too Long", 35, null, null, new List<string>()), // > 30 verses
            new(42, "Allowed", 20, null, null, new List<string>())
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository());
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 2, 1), 1);

        Assert.Single(plans);
        Assert.Equal(42, plans[0].PsalmId);
    }

    [Fact]
    public async Task First_Sunday_Falls_Back_When_No_Alabanza()
    {
        var psalms = new List<Psalm>
        {
            new(1, "General Low Reads", 20, null, null, new List<string>()),
            new(2, "General More Reads", 20, null, null, new List<string>())
        };

        var pastReads = new List<ReadingRecord>
        {
            new(Guid.NewGuid(), 2, new DateOnly(2024, 1, 7)),
            new(Guid.NewGuid(), 2, new DateOnly(2024, 2, 7)),
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(pastReads));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 5, 1), 1);

        Assert.Contains(plans, p => p.PsalmId == 1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task Throws_When_Months_Not_Positive(int months)
    {
        var scheduler = new ReadingScheduler(new FakePsalmRepository(new List<Psalm>()), new FakeReadingRepository());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => scheduler.GenerateScheduleAsync(new DateOnly(2025, 1, 1), months));
    }

    [Fact]
    public async Task Stops_When_Psalms_Are_Exhausted()
    {
        var psalms = new List<Psalm>
        {
            new(5, "Only Psalm", 20, null, null, new List<string>())
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository());
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 1, 12), 3);

        Assert.Single(plans);
        Assert.Equal(5, plans[0].PsalmId);
    }

    [Fact]
    public async Task Orders_By_ReadCounts_Then_Verses_Then_Id()
    {
        var psalms = new List<Psalm>
        {
            new(10, "Most Read", 20, null, null, new List<string>()),
            new(11, "Tie One", 10, null, null, new List<string>()),
            new(12, "Tie Two", 10, null, null, new List<string>()),
            new(13, "Never Read", 15, null, null, new List<string>())
        };

        var reads = new List<ReadingRecord>
        {
            new(Guid.NewGuid(), 10, new DateOnly(2024, 1, 1)),
            new(Guid.NewGuid(), 10, new DateOnly(2024, 1, 8)),
            new(Guid.NewGuid(), 10, new DateOnly(2024, 1, 15)),
            new(Guid.NewGuid(), 10, new DateOnly(2024, 1, 22)),
            new(Guid.NewGuid(), 10, new DateOnly(2024, 1, 29)),
            new(Guid.NewGuid(), 11, new DateOnly(2024, 2, 5)),
            new(Guid.NewGuid(), 12, new DateOnly(2024, 2, 12))
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(reads));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 3, 9), 1);

        var orderedIds = plans.Select(p => p.PsalmId).ToList();
        Assert.Equal(new List<int> { 13, 11, 12, 10 }, orderedIds);
    }

    [Fact]
    public async Task Uses_HolyWeek_Psalms_For_All_Sundays()
    {
        var psalms = new List<Psalm>
        {
            new(200, "General", 10, null, null, new List<string>()),
            new(113, "Palm", 10, null, null, new List<string>()),
            new(114, "Easter", 10, null, null, new List<string>()),
            new(115, "After Easter", 10, null, null, new List<string>()),
            new(300, "Alabanza", 10, "alabanza", null, new List<string>())
        };

        var reads = new List<ReadingRecord>
        {
            new(Guid.NewGuid(), 113, new DateOnly(2024, 4, 14)),
            new(Guid.NewGuid(), 113, new DateOnly(2024, 4, 21)),
            new(Guid.NewGuid(), 113, new DateOnly(2024, 4, 28)),
            new(Guid.NewGuid(), 115, new DateOnly(2024, 4, 21)),
            new(Guid.NewGuid(), 115, new DateOnly(2024, 4, 28))
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(reads));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 4, 7), 1);

        var plansByDate = plans.ToDictionary(p => p.ScheduledDate, p => p.PsalmId);

        Assert.Equal(114, plansByDate[new DateOnly(2025, 4, 13)]);
        Assert.Equal(115, plansByDate[new DateOnly(2025, 4, 20)]);
        Assert.Equal(113, plansByDate[new DateOnly(2025, 4, 27)]);
        Assert.Equal(300, plansByDate[new DateOnly(2025, 5, 4)]);
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

        public Task<bool> UpdateAsync(ReadingRecord record, CancellationToken cancellationToken = default)
        {
            var index = _records.FindIndex(r => r.Id == record.Id);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            _records[index] = record;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var removed = _records.RemoveAll(r => r.Id == id) > 0;
            return Task.FromResult(removed);
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
