using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Application.Interfaces;

public interface ICalendarExporter
{
    Task<string> CreateCalendarAsync(IEnumerable<PlannedReading> plannedReadings, CancellationToken cancellationToken = default);
}
