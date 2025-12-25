using PsalmsReading.Application.Models;

namespace PsalmsReading.Application.Interfaces;

public interface IReadingCalendarImportService
{
    public Task<ReadingExportDto> ParseAsync(string icsContent, CancellationToken cancellationToken = default);
}
