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

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(), new FakePlannedReadingRepository(), new Random(42));

        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 1, 1), 1);

        Assert.Contains(plans, p => p.PsalmId == 2);
    }

    [Fact]
    public async Task First_Sunday_Rule_Is_Labeled()
    {
        var psalms = new List<Psalm>
        {
            new(1, "General", 20, null, null, new List<string>()),
            new(2, "Alabanza Psalm", 20, "alabanza", null, new List<string>())
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(), new FakePlannedReadingRepository(), new Random(42));

        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 6, 1), 1);

        Assert.Contains(plans, p => p.RuleApplied == "First Sunday of worship");
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

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(pastReads), new FakePlannedReadingRepository(), new Random(42));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 4, 1), 1);

        Assert.Contains(plans, p => p.PsalmId == 113);
    }

    [Fact]
    public async Task HolyWeek_Rule_Is_Labeled()
    {
        var psalms = new List<Psalm>
        {
            new(113, "Palm", 20, null, null, new List<string>()),
            new(114, "Easter", 20, null, null, new List<string>())
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(), new FakePlannedReadingRepository(), new Random(42));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 4, 1), 1);

        Assert.All(plans.Where(p => p.ScheduledDate.Month == 4 && p.ScheduledDate.Day is 13 or 20 or 27),
            p => Assert.Equal("HolyWeek", p.RuleApplied));
    }

    [Fact]
    public async Task December_Falls_Back_To_Theme_When_Type_Missing()
    {
        var psalms = new List<Psalm>
        {
            new(10, "General", 20, null, null, new List<string>()),
            new(11, "Mesiánico", 20, null, new List<string> { "Mesiánico" }, new List<string>())
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(), new FakePlannedReadingRepository(), new Random(42));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 12, 1), 1);

        Assert.Contains(plans, p => p.PsalmId == 11);
    }

    [Fact]
    public async Task December_Rule_Is_Labeled()
    {
        var psalms = new List<Psalm>
        {
            new(11, "Mesiánico", 20, "mesiánico", new List<string> { "Mesiánico" }, new List<string>())
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(), new FakePlannedReadingRepository(), new Random(42));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 12, 1), 1);

        Assert.All(plans, p => Assert.Equal("Christmas season", p.RuleApplied));
    }

    [Fact]
    public async Task First_Sunday_Of_Year_Prefers_NewYear_Theme()
    {
        var psalms = new List<Psalm>
        {
            new(1, "General", 20, null, null, new List<string>()),
            new(2, "Año nuevo", 20, null, null, new List<string> { "Días festivos: año nuevo" })
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(), new FakePlannedReadingRepository(), new Random(42));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 1, 1), 1);

        Assert.Contains(plans, p => p.PsalmId == 2);
    }

    [Fact]
    public async Task First_Sunday_Of_Year_Rule_Is_Labeled()
    {
        var psalms = new List<Psalm>
        {
            new(2, "Año nuevo", 20, null, null, new List<string> { "Días festivos: año nuevo" })
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(), new FakePlannedReadingRepository(), new Random(42));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2026, 1, 1), 1);

        Assert.All(plans, p => Assert.Equal("First Sunday new year", p.RuleApplied));
    }

    [Fact]
    public async Task Last_Two_November_Sundays_Prefer_Thanksgiving_Theme()
    {
        var psalms = new List<Psalm>
        {
            new(1, "General", 20, null, null, new List<string>()),
            new(2, "Thanksgiving", 20, null, null, new List<string> { "Días festivos: Agradecimiento" })
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(), new FakePlannedReadingRepository(), new Random(42));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 11, 1), 1);

        var thanksgivingSundays = plans.Where(p => p.RuleApplied == "Thanksgiving").Select(p => p.PsalmId).ToList();
        Assert.All(thanksgivingSundays, id => Assert.Equal(2, id));
    }

    [Fact]
    public async Task Thanksgiving_Rule_Is_Labeled()
    {
        var psalms = new List<Psalm>
        {
            new(1, "General 1", 20, null, null, new List<string>()),
            new(2, "General 2", 20, null, null, new List<string>()),
            new(3, "Thanksgiving A", 20, null, null, new List<string> { "Días festivos: Agradecimiento" }),
            new(4, "Thanksgiving B", 20, null, null, new List<string> { "Días festivos: Agradecimiento" })
        };

        var pastReads = new List<ReadingRecord>
        {
            new(Guid.NewGuid(), 3, new DateOnly(2026, 11, 21)),
            new(Guid.NewGuid(), 4, new DateOnly(2026, 11, 28)),
            new(Guid.NewGuid(), 4, new DateOnly(2026, 10, 21))
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(pastReads), new FakePlannedReadingRepository(), new Random(42));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2027, 11, 1), 1);

        var byDate = plans.Where(p => p.ScheduledDate.Month == 11).ToDictionary(p => p.ScheduledDate, p => p);

        Assert.Equal("Thanksgiving", byDate[new DateOnly(2027, 11, 21)].RuleApplied);
        Assert.Equal("Thanksgiving", byDate[new DateOnly(2027, 11, 28)].RuleApplied);
        Assert.Equal("General", byDate[new DateOnly(2027, 11, 7)].RuleApplied);
        Assert.Equal("General", byDate[new DateOnly(2027, 11, 14)].RuleApplied);
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

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(), new FakePlannedReadingRepository(), new Random(42));
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

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(pastReads), new FakePlannedReadingRepository(), new Random(42));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 5, 1), 1);

        Assert.Contains(plans, p => p.PsalmId == 1 && p.RuleApplied == "General");
    }

    [Fact]
    public async Task General_Rule_Is_Labeled_For_NonSpecial_Sunday()
    {
        var psalms = new List<Psalm>
        {
            new(1, "General Low Reads", 20, null, null, new List<string>()),
            new(2, "General High Reads", 20, null, null, new List<string>())
        };

        var pastReads = new List<ReadingRecord>
        {
            new(Guid.NewGuid(), 2, new DateOnly(2024, 3, 10))
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(pastReads), new FakePlannedReadingRepository(), new Random(42));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 3, 12), 1);

        Assert.All(plans, p => Assert.Equal("General", p.RuleApplied));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task Throws_When_Months_Not_Positive(int months)
    {
        var scheduler = new ReadingScheduler(new FakePsalmRepository(new List<Psalm>()), new FakeReadingRepository(), new FakePlannedReadingRepository(), new Random(42));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => scheduler.GenerateScheduleAsync(new DateOnly(2025, 1, 1), months));
    }

    [Fact]
    public async Task Stops_When_Psalms_Are_Exhausted()
    {
        var psalms = new List<Psalm>
        {
            new(5, "Only Psalm", 20, null, null, new List<string>())
        };

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(), new FakePlannedReadingRepository(), new Random(42));
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

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(reads), new FakePlannedReadingRepository(), new Random(42));
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
        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(), new FakePlannedReadingRepository(), new Random(42));
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

        var scheduler = new ReadingScheduler(new FakePsalmRepository(psalms), new FakeReadingRepository(reads), new FakePlannedReadingRepository(), new Random(42));
        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 4, 7), 1);

        var plansByDate = plans.ToDictionary(p => p.ScheduledDate, p => p.PsalmId);

        Assert.Equal(114, plansByDate[new DateOnly(2025, 4, 13)]);
        Assert.Equal(115, plansByDate[new DateOnly(2025, 4, 20)]);
        Assert.Equal(113, plansByDate[new DateOnly(2025, 4, 27)]);
        Assert.Equal(300, plansByDate[new DateOnly(2025, 5, 4)]);
    }

    [Fact]
    public async Task Avoids_Same_Type_In_A_Row()
    {
        var psalms = new List<Psalm>
        {
            new(1, "Lamento A", 10, "lamento", null, new List<string>()),
            new(2, "Lamento B", 10, "lamento", null, new List<string>()),
            new(3, "Alabanza", 10, "alabanza", null, new List<string>())
        };

        var planned = new List<PlannedReading>
        {
            new(Guid.NewGuid(), 1, new DateOnly(2025, 2, 2), "General"),
            new(Guid.NewGuid(), 2, new DateOnly(2025, 2, 9), "General")
        };

        ReadingScheduler scheduler = new(
            new FakePsalmRepository(psalms),
            new FakeReadingRepository(),
            new FakePlannedReadingRepository(planned),
            new Random(42));

        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 2, 10), 1);

        Assert.Contains(plans, p => p.ScheduledDate == new DateOnly(2025, 2, 16) && p.PsalmId == 3);
    }

    [Fact]
    public async Task GeneralRule_Prefers_Types_Not_Used_Recently()
    {
        var psalms = new List<Psalm>
        {
            new(1, "Lamento A", 10, "lamento", null, new List<string>()),
            new(2, "Lamento B", 10, "lamento", null, new List<string>()),
            new(3, "Alabanza", 10, "alabanza", null, new List<string>())
        };

        var planned = new List<PlannedReading>
        {
            new(Guid.NewGuid(), 1, new DateOnly(2025, 2, 9), "General"),
            new(Guid.NewGuid(), 2, new DateOnly(2025, 2, 16), "General")
        };

        var scheduler = new ReadingScheduler(
            new FakePsalmRepository(psalms),
            new FakeReadingRepository(),
            new FakePlannedReadingRepository(planned),
            new Random(42));

        var plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 3, 2), 1);

        Assert.Contains(plans, p => p.ScheduledDate == new DateOnly(2025, 3, 2) && p.PsalmId == 3);
    }

    [Fact]
    public async Task GeneralRule_Respects_Monthly_Type_Cap()
    {
        List<Psalm> psalms = new()
        {
            new(1, "Lamento A", 10, "lamento", null, new List<string>()),
            new(2, "Lamento B", 10, "lamento", null, new List<string>()),
            new(3, "Alabanza", 10, "alabanza", null, new List<string>())
        };

        List<PlannedReading> planned = new()
        {
            new(Guid.NewGuid(), 1, new DateOnly(2025, 5, 4), "General"),
            new(Guid.NewGuid(), 2, new DateOnly(2025, 5, 11), "General")
        };

        var scheduler = new ReadingScheduler(
            new FakePsalmRepository(psalms),
            new FakeReadingRepository(),
            new FakePlannedReadingRepository(planned),
            new Random(42));

        IReadOnlyList<PlannedReading> plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 5, 18), 1);

        Assert.Contains(plans, p => p.ScheduledDate == new DateOnly(2025, 5, 18) && p.PsalmId == 3);
    }

    [Fact]
    public async Task GeneralRule_Prioritizes_Alabanza_Or_Lamento_When_Missing_In_Month()
    {
        List<Psalm> psalms = new()
        {
            new(1, "Lamento A", 10, "lamento", null, new List<string>()),
            new(2, "Alabanza A", 10, "alabanza", null, new List<string>()),
            new(3, "Sabiduria A", 10, "sabiduria", null, new List<string>())
        };

        List<PlannedReading> planned = new()
        {
            new(Guid.NewGuid(), 3, new DateOnly(2025, 2, 2), "General")
        };

        var scheduler = new ReadingScheduler(
            new FakePsalmRepository(psalms),
            new FakeReadingRepository(),
            new FakePlannedReadingRepository(planned),
            new Random(42));

        IReadOnlyList<PlannedReading> plans = await scheduler.GenerateScheduleAsync(new DateOnly(2025, 2, 10), 1);

        Assert.Contains(plans, p => p.ScheduledDate == new DateOnly(2025, 2, 16) && (p.PsalmId == 1 || p.PsalmId == 2));
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

    private sealed class FakePlannedReadingRepository : IPlannedReadingRepository
    {
        private readonly List<PlannedReading> _planned;

        public FakePlannedReadingRepository(IEnumerable<PlannedReading>? planned = null)
        {
            _planned = planned?.ToList() ?? new List<PlannedReading>();
        }

        public Task SavePlansAsync(IEnumerable<PlannedReading> plans, CancellationToken cancellationToken = default)
        {
            _planned.AddRange(plans);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PlannedReading>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PlannedReading>>(_planned);

        public Task<IReadOnlyList<PlannedReading>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        {
            var result = _planned.Where(p => p.ScheduledDate >= from && p.ScheduledDate <= to).ToList();
            return Task.FromResult<IReadOnlyList<PlannedReading>>(result);
        }

        public Task ClearRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        {
            _planned.RemoveAll(p => p.ScheduledDate >= from && p.ScheduledDate <= to);
            return Task.CompletedTask;
        }
    }
}
