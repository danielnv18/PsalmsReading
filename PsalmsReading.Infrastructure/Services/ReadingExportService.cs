using PsalmsReading.Application.Interfaces;
using PsalmsReading.Application.Models;
using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Services;

public sealed class ReadingExportService : IReadingExportService
{
    private const int CurrentSchemaVersion = 1;
    private readonly IReadingRepository _readingRepository;

    public ReadingExportService(IReadingRepository readingRepository)
    {
        _readingRepository = readingRepository;
    }

    public async Task<IReadOnlyList<ReadingRecord>> GetReadingsAsync(
        ReadingExportRange range,
        int? year,
        CancellationToken cancellationToken = default)
    {
        if (range == ReadingExportRange.All)
        {
            IReadOnlyList<ReadingRecord> all = await _readingRepository.GetAllAsync(cancellationToken);
            return all.OrderBy(r => r.DateRead).ToList();
        }

        int targetYear = year ?? DateTime.Today.Year;
        DateOnly start = new(targetYear, 1, 1);
        DateOnly end = new(targetYear, 12, 31);
        IReadOnlyList<ReadingRecord> rangeReadings = await _readingRepository.GetByDateRangeAsync(start, end, cancellationToken);
        return rangeReadings.OrderBy(r => r.DateRead).ToList();
    }

    public async Task<ReadingExportDto> ExportJsonAsync(
        ReadingExportRange range,
        int? year,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ReadingRecord> readings = await GetReadingsAsync(range, year, cancellationToken);
        List<ReadingExportRecordDto> records = readings
            .Select(r => new ReadingExportRecordDto(r.Id, r.PsalmId, r.DateRead, r.RuleApplied))
            .ToList();

        string rangeValue = range == ReadingExportRange.All ? "all" : "year";
        int? rangeYear = range == ReadingExportRange.Year ? year ?? DateTime.Today.Year : null;

        return new ReadingExportDto(
            CurrentSchemaVersion,
            DateTimeOffset.UtcNow,
            rangeValue,
            rangeYear,
            records);
    }
}
