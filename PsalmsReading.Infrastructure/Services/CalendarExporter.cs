using System.Text;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using PsalmsReading.Application.Interfaces;
using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Services;

public sealed class CalendarExporter : ICalendarExporter
{
    private readonly IPsalmRepository _psalmRepository;

    public CalendarExporter(IPsalmRepository psalmRepository)
    {
        _psalmRepository = psalmRepository;
    }

    public async Task<string> CreateCalendarAsync(IEnumerable<PlannedReading> plannedReadings, CancellationToken cancellationToken = default)
    {
        var plans = plannedReadings.OrderBy(p => p.ScheduledDate).ToList();
        if (plans.Count == 0)
        {
            return string.Empty;
        }

        var neededPsalmIds = plans.Select(p => p.PsalmId).Distinct().ToHashSet();
        var psalms = (await _psalmRepository.GetAllAsync(cancellationToken))
            .Where(p => neededPsalmIds.Contains(p.Id))
            .ToDictionary(p => p.Id);

        var calendar = new Calendar { Method = "PUBLISH" };

        foreach (var plan in plans)
        {
            if (!psalms.TryGetValue(plan.PsalmId, out var psalm))
            {
                continue;
            }

            var start = new CalDateTime(plan.ScheduledDate.Year, plan.ScheduledDate.Month, plan.ScheduledDate.Day);

            var ev = new CalendarEvent
            {
                Summary = $"Salmo {psalm.Id} - {psalm.Title}",
                Description = BuildDescription(psalm),
                DtStart = start,
                DtEnd = start.AddDays(1),
            };

            calendar.Events.Add(ev);
        }

        var serializer = new CalendarSerializer();
        return serializer.SerializeToString(calendar) ?? string.Empty;
    }

    private static string BuildDescription(Psalm psalm)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Salmo {psalm.Id} - {psalm.Title}");
        builder.AppendLine($"Categoria: {psalm.Type ?? "Sin categoria"}");
        builder.Append($"Link: https://www.bible.com/bible/103/PSA.{psalm.Id}.NBLA");
        return builder.ToString();
    }
}
