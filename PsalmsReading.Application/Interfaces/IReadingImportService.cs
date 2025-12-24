using PsalmsReading.Application.Models;

namespace PsalmsReading.Application.Interfaces;

public interface IReadingImportService
{
    Task<ReadingImportPreviewDto> PreviewAsync(
        ReadingExportDto importData,
        CancellationToken cancellationToken = default);

    Task<ReadingImportResultDto> ImportAsync(
        ReadingExportDto importData,
        ReadingImportMode mode,
        CancellationToken cancellationToken = default);
}
