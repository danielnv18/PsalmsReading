using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Application.Interfaces;

public interface ICalendarExporter
{
    string CreateCalendar(IEnumerable<PlannedReading> plannedReadings);
}
