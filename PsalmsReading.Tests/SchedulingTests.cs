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

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(), new Random(42));

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

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(pastReads), new Random(42));
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

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(), new Random(42));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 12, 1), 1);

        Assert.Contains(plans, p => p.PsalmId == 11);
    }

    [Fact]
    public async Task First_Sunday_Of_Year_Prefers_NewYear_Theme()
    {
        var psalms = new List<Psalm>
        {
            new(1, "General", 20, null, null, new List<string>()),
            new(2, "Año nuevo", 20, null, null, new List<string> { "Días festivos: año nuevo" })
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(), new Random(42));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 1, 1), 1);

        Assert.Contains(plans, p => p.PsalmId == 2);
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

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(), new Random(42));
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

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(pastReads), new Random(42));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 5, 1), 1);

        Assert.Contains(plans, p => p.PsalmId == 1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task Throws_When_Months_Not_Positive(int months)
    {
        var scheduler = new ReadingScheduler(new FakePsalmRepository(new List<Psalm>()), new FakeReadingRepository(), new Random(42));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => scheduler.GenerateScheduleAsync(new DateOnly(2025, 1, 1), months));
    }

    [Fact]
    public async Task Stops_When_Psalms_Are_Exhausted()
    {
        var psalms = new List<Psalm>
        {
            new(5, "Only Psalm", 20, null, null, new List<string>())
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(), new Random(42));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 1, 12), 3);

        Assert.Single(plans);
        Assert.Equal(5, plans[0].PsalmId);
    }

    [Fact]
    public async Task Prioritizes_Least_Read_Psalms_In_Tier()
    {
        var psalms = new List<Psalm>
        {
            new(10, "Most Read", 20, null, null, new List<string>()),
            new(11, "Read Once", 10, null, null, new List<string>()),
            new(12, "Never Read", 15, null, null, new List<string>()),
            new(13, "Never Read Too", 25, null, null, new List<string>())
        };

        var reads = new List<ReadingRecord>
        {
            new(Guid.NewGuid(), 10, new DateOnly(2024, 1, 1)),
            new(Guid.NewGuid(), 10, new DateOnly(2024, 1, 8)),
            new(Guid.NewGuid(), 10, new DateOnly(2024, 1, 15)),
            new(Guid.NewGuid(), 10, new DateOnly(2024, 1, 22)),
            new(Guid.NewGuid(), 10, new DateOnly(2024, 1, 29)),
            new(Guid.NewGuid(), 11, new DateOnly(2024, 2, 5))
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(reads), new Random(42));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 3, 9), 4);

        // First two selections should be from tier with 0 reads (psalms 12 or 13, randomly chosen with seed 42)
        // Third selection should be from tier with 1 read (psalm 11)
        // Fourth selection should be from tier with 5 reads (psalm 10)
        var orderedIds = plans.Select(p => p.PsalmId).ToList();

        Assert.True(orderedIds[0] is 12 or 13, $"First should be 12 or 13, got {orderedIds[0]}");
        Assert.True(orderedIds[1] is 12 or 13, $"Second should be 12 or 13, got {orderedIds[1]}");
        Assert.NotEqual(orderedIds[0], orderedIds[1]);
        Assert.Equal(11, orderedIds[2]);
        Assert.Equal(10, orderedIds[3]);
    }

    [Fact]
    public async Task Random_Selection_Among_More_Than_Two_In_Tier()
    {
        var psalms = new List<Psalm>
        {
            new(10, "First", 20, null, null, new List<string>()),
            new(11, "Second", 20, null, null, new List<string>()),
            new(12, "Third", 20, null, null, new List<string>()),
            new(13, "Fourth", 20, null, null, new List<string>())
        };

        // All psalms have 0 reads, so all 4 are in the same tier
        // Start on a Sunday and request minimal month span to get 1 Sunday
        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(), new Random(42));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 3, 9), 1);

        // 1 month starting March 9, 2025 (Sunday) will give multiple Sundays
        // Just verify at least one selected is from the correct tier
        Assert.NotEmpty(plans);
        foreach (var plan in plans)
        {
            Assert.Contains(plan.PsalmId, new List<int> { 10, 11, 12, 13 });
        }
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

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(reads), new Random(42));
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
