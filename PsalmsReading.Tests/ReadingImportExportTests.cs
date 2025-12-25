using PsalmsReading.Application.Interfaces;
using PsalmsReading.Application.Models;
using PsalmsReading.Domain.Entities;
using PsalmsReading.Infrastructure.Services;

namespace PsalmsReading.Tests;

public sealed class ReadingImportExportTests
{
    [Fact]
    public async Task ReadingExportService_Year_ReturnsOnlyThatYear()
    {
        List<ReadingRecord> records = new()
        {
            new ReadingRecord(Guid.NewGuid(), 1, new DateOnly(2025, 1, 5), null),
            new ReadingRecord(Guid.NewGuid(), 2, new DateOnly(2024, 12, 29), null),
            new ReadingRecord(Guid.NewGuid(), 3, new DateOnly(2025, 6, 8), null)
        };

        FakeReadingRepository repo = new(records);
        ReadingExportService service = new(repo);

        ReadingExportDto export = await service.ExportJsonAsync(ReadingExportRange.Year, 2025);

        Assert.Equal("year", export.Range);
        Assert.Equal(2025, export.Year);
        Assert.Equal(2, export.Records.Count);
        Assert.All(export.Records, record => Assert.Equal(2025, record.DateRead.Year));
    }

    [Fact]
    public async Task ReadingImportService_Preview_DetectsConflicts()
    {
        DateOnly conflictDate = new(2025, 1, 5);
        List<ReadingRecord> existing = new()
        {
            new ReadingRecord(Guid.NewGuid(), 10, conflictDate, null)
        };
        FakeReadingRepository repo = new(existing);
        ReadingImportService service = new(repo);

        ReadingExportDto importData = new(
            1,
            DateTimeOffset.UtcNow,
            "all",
            null,
            new List<ReadingExportRecordDto>
            {
                new ReadingExportRecordDto(Guid.Empty, 27, conflictDate, null)
            });

        ReadingImportPreviewDto preview = await service.PreviewAsync(importData);

        Assert.Equal(1, preview.ConflictCount);
        Assert.Contains(conflictDate, preview.ConflictDates);
    }

    [Fact]
    public async Task ReadingImportService_IgnoreConflicts_SkipsExistingDates()
    {
        DateOnly conflictDate = new(2025, 2, 2);
        DateOnly newDate = new(2025, 2, 9);
        List<ReadingRecord> existing = new()
        {
            new ReadingRecord(Guid.NewGuid(), 10, conflictDate, null)
        };
        FakeReadingRepository repo = new(existing);
        ReadingImportService service = new(repo);

        ReadingExportDto importData = new(
            1,
            DateTimeOffset.UtcNow,
            "all",
            null,
            new List<ReadingExportRecordDto>
            {
                new ReadingExportRecordDto(Guid.Empty, 27, conflictDate, null),
                new ReadingExportRecordDto(Guid.Empty, 28, newDate, null)
            });

        ReadingImportResultDto result = await service.ImportAsync(importData, ReadingImportMode.IgnoreConflicts);

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(2, repo.Stored.Count);
        Assert.Contains(repo.Stored, r => r.DateRead == newDate);
    }

    [Fact]
    public async Task ReadingImportService_ReplaceConflicts_ReplacesDates()
    {
        DateOnly conflictDate = new(2025, 3, 2);
        List<ReadingRecord> existing = new()
        {
            new ReadingRecord(Guid.NewGuid(), 10, conflictDate, null)
        };
        FakeReadingRepository repo = new(existing);
        ReadingImportService service = new(repo);

        ReadingExportDto importData = new(
            1,
            DateTimeOffset.UtcNow,
            "all",
            null,
            new List<ReadingExportRecordDto>
            {
                new ReadingExportRecordDto(Guid.Empty, 50, conflictDate, null)
            });

        ReadingImportResultDto result = await service.ImportAsync(importData, ReadingImportMode.ReplaceConflicts);

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Single(repo.Stored);
        Assert.Equal(50, repo.Stored[0].PsalmId);
    }

    [Fact]
    public async Task ReadingCalendarImportService_ParsesGoogleIcs()
    {
        const string ics = """
BEGIN:VCALENDAR
VERSION:2.0
BEGIN:VEVENT
DTSTART;VALUE=DATE:20250810
DTEND;VALUE=DATE:20250811
SUMMARY:Salmo 66 - Agradecimiento a Dios por sus obras
DESCRIPTION:Salmo 66 - Agradecimiento a Dios por sus obras\nCategoria: realeza\nLink: https://www.bible.com/bible/103/PSA.66.NBLA
END:VEVENT
END:VCALENDAR
""";

        ReadingCalendarImportService service = new();

        ReadingExportDto export = await service.ParseAsync(ics);

        Assert.Single(export.Records);
        ReadingExportRecordDto record = export.Records[0];
        Assert.Equal(66, record.PsalmId);
        Assert.Equal(new DateOnly(2025, 8, 10), record.DateRead);
    }

    private sealed class FakeReadingRepository : IReadingRepository
    {
        public List<ReadingRecord> Stored { get; } = new();

        public FakeReadingRepository(IEnumerable<ReadingRecord> records)
        {
            Stored.AddRange(records);
        }

        public Task AddAsync(ReadingRecord record, CancellationToken cancellationToken = default)
        {
            Stored.Add(record);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<ReadingRecord> records, CancellationToken cancellationToken = default)
        {
            Stored.AddRange(records);
            return Task.CompletedTask;
        }

        public Task<bool> UpdateAsync(ReadingRecord record, CancellationToken cancellationToken = default)
        {
            int index = Stored.FindIndex(r => r.Id == record.Id);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            Stored[index] = record;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            bool removed = Stored.RemoveAll(r => r.Id == id) > 0;
            return Task.FromResult(removed);
        }

        public Task<int> DeleteByDatesAsync(IReadOnlyList<DateOnly> dates, CancellationToken cancellationToken = default)
        {
            if (dates.Count == 0)
            {
                return Task.FromResult(0);
            }

            HashSet<DateOnly> dateSet = dates.Distinct().ToHashSet();
            int removed = Stored.RemoveAll(r => dateSet.Contains(r.DateRead));
            return Task.FromResult(removed);
        }

        public Task<IReadOnlyList<ReadingRecord>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ReadingRecord>>(Stored);

        public Task<IReadOnlyList<ReadingRecord>> GetByDateRangeAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
        {
            List<ReadingRecord> result = Stored.Where(r => r.DateRead >= start && r.DateRead <= end).ToList();
            return Task.FromResult<IReadOnlyList<ReadingRecord>>(result);
        }

        public Task<int> GetReadCountAsync(int psalmId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored.Count(r => r.PsalmId == psalmId));

        public Task ClearRangeAsync(DateOnly start, DateOnly end, DateOnly? minDateInclusive = default, CancellationToken cancellationToken = default)
        {
            IEnumerable<ReadingRecord> filtered = Stored.Where(r => r.DateRead >= start && r.DateRead <= end);
            if (minDateInclusive.HasValue)
            {
                DateOnly minDate = minDateInclusive.Value;
                filtered = filtered.Where(r => r.DateRead >= minDate);
            }

            List<ReadingRecord> toRemove = filtered.ToList();
            foreach (ReadingRecord record in toRemove)
            {
                Stored.Remove(record);
            }

            return Task.CompletedTask;
        }
    }
}
