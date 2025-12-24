using PsalmsReading.Application.Models;
using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Application.Interfaces;

public interface IReadingExportService
{
    Task<IReadOnlyList<ReadingRecord>> GetReadingsAsync(
        ReadingExportRange range,
        int? year,
        CancellationToken cancellationToken = default);

    Task<ReadingExportDto> ExportJsonAsync(
        ReadingExportRange range,
        int? year,
        CancellationToken cancellationToken = default);
}
