using PsalmsReading.Application.Interfaces;
using PsalmsReading.Application.Models;
using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Services;

public sealed class ReadingImportService : IReadingImportService
{
    private const int CurrentSchemaVersion = 1;
    private readonly IReadingRepository _readingRepository;

    public ReadingImportService(IReadingRepository readingRepository)
    {
        _readingRepository = readingRepository;
    }

    public async Task<ReadingImportPreviewDto> PreviewAsync(
        ReadingExportDto importData,
        CancellationToken cancellationToken = default)
    {
        List<ReadingExportRecordDto> normalized = Normalize(importData);
        if (normalized.Count == 0)
        {
            return new ReadingImportPreviewDto(0, 0, Array.Empty<DateOnly>());
        }

        IReadOnlyList<DateOnly> conflictDates = await GetConflictDatesAsync(normalized, cancellationToken);
        return new ReadingImportPreviewDto(normalized.Count, conflictDates.Count, conflictDates);
    }

    public async Task<ReadingImportResultDto> ImportAsync(
        ReadingExportDto importData,
        ReadingImportMode mode,
        CancellationToken cancellationToken = default)
    {
        List<ReadingExportRecordDto> normalized = Normalize(importData);
        if (normalized.Count == 0)
        {
            return new ReadingImportResultDto(0, 0, 0);
        }

        IReadOnlyList<DateOnly> conflictDates = await GetConflictDatesAsync(normalized, cancellationToken);
        HashSet<DateOnly> conflictSet = conflictDates.ToHashSet();
        int replacedDates = 0;

        if (mode == ReadingImportMode.ReplaceConflicts && conflictSet.Count > 0)
        {
            foreach (DateOnly conflict in conflictSet)
            {
                await _readingRepository.ClearRangeAsync(conflict, conflict, null, cancellationToken);
            }

            replacedDates = conflictSet.Count;
        }

        List<ReadingExportRecordDto> toImport = mode == ReadingImportMode.IgnoreConflicts
            ? normalized.Where(r => !conflictSet.Contains(r.DateRead)).ToList()
            : normalized;

        List<ReadingRecord> domainRecords = toImport
            .Select(r =>
            {
                Guid id = r.Id == Guid.Empty ? Guid.NewGuid() : r.Id;
                return new ReadingRecord(id, r.PsalmId, r.DateRead, r.RuleApplied);
            })
            .ToList();

        await _readingRepository.AddRangeAsync(domainRecords, cancellationToken);

        int skippedCount = normalized.Count - toImport.Count;
        return new ReadingImportResultDto(domainRecords.Count, skippedCount, replacedDates);
    }

    private static List<ReadingExportRecordDto> Normalize(ReadingExportDto importData)
    {
        if (importData is null)
        {
            throw new ArgumentNullException(nameof(importData));
        }

        if (importData.SchemaVersion != CurrentSchemaVersion)
        {
            throw new ArgumentException($"Unsupported schema version: {importData.SchemaVersion}.");
        }

        if (importData.Records is null)
        {
            throw new ArgumentException("Records are required.");
        }

        Dictionary<(int PsalmId, DateOnly DateRead), ReadingExportRecordDto> unique = new();
        foreach (ReadingExportRecordDto record in importData.Records)
        {
            if (record.PsalmId <= 0)
            {
                throw new ArgumentException("PsalmId must be greater than 0.");
            }

            if (record.DateRead == default)
            {
                throw new ArgumentException("DateRead is required.");
            }

            unique[(record.PsalmId, record.DateRead)] = record;
        }

        return unique.Values.OrderBy(r => r.DateRead).ToList();
    }

    private async Task<IReadOnlyList<DateOnly>> GetConflictDatesAsync(
        IReadOnlyList<ReadingExportRecordDto> records,
        CancellationToken cancellationToken)
    {
        DateOnly minDate = records.Min(r => r.DateRead);
        DateOnly maxDate = records.Max(r => r.DateRead);

        IReadOnlyList<ReadingRecord> existing = await _readingRepository.GetByDateRangeAsync(minDate, maxDate, cancellationToken);
        HashSet<DateOnly> existingDates = existing.Select(r => r.DateRead).ToHashSet();

        List<DateOnly> conflicts = records
            .Select(r => r.DateRead)
            .Distinct()
            .Where(existingDates.Contains)
            .OrderBy(d => d)
            .ToList();

        return conflicts;
    }
}
