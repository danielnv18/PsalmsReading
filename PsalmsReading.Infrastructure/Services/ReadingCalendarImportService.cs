using System.Text.RegularExpressions;
using Ical.Net;
using PsalmsReading.Application.Interfaces;
using PsalmsReading.Application.Models;

namespace PsalmsReading.Infrastructure.Services;

public sealed class ReadingCalendarImportService : IReadingCalendarImportService
{
    private const int CurrentSchemaVersion = 1;
    private static readonly Regex PsalmRegex = new(@"Salmo\s+(?<id>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PsalmLinkRegex = new(@"PSA\.(?<id>\d+)\.", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PsalmEnglishRegex = new(@"Psalm\s+(?<id>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public Task<ReadingExportDto> ParseAsync(string icsContent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(icsContent))
        {
            throw new ArgumentException("ICS content is required.", nameof(icsContent));
        }

        Ical.Net.Calendar? calendar = Calendar.Load(icsContent);
        if (calendar is null)
        {
            throw new ArgumentException("No calendar data found in ICS.");
        }

        List<Ical.Net.CalendarComponents.CalendarEvent> events = calendar.Events.ToList();

        if (events.Count == 0)
        {
            throw new ArgumentException("No calendar data found in ICS.");
        }

        List<ReadingExportRecordDto> records = new();
        foreach (Ical.Net.CalendarComponents.CalendarEvent calendarEvent in events)
        {
            if (calendarEvent.DtStart is null)
            {
                continue;
            }

            int psalmId = ExtractPsalmId(calendarEvent.Summary, calendarEvent.Description);
            if (psalmId <= 0)
            {
                throw new ArgumentException("Unable to parse psalm id from ICS event summary.");
            }

            DateOnly dateRead = DateOnly.FromDateTime(calendarEvent.DtStart.Value.Date);
            records.Add(new ReadingExportRecordDto(Guid.Empty, psalmId, dateRead, null));
        }

        ReadingExportDto export = new ReadingExportDto(
            CurrentSchemaVersion,
            DateTimeOffset.UtcNow,
            "all",
            null,
            records);

        return Task.FromResult(export);
    }

    private static int ExtractPsalmId(string? summary, string? description)
    {
        int id = TryMatchPsalm(summary);
        if (id > 0)
        {
            return id;
        }

        id = TryMatchPsalm(description);
        if (id > 0)
        {
            return id;
        }

        return TryMatchPsalmFromLink(description);
    }

    private static int TryMatchPsalm(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        Match match = PsalmRegex.Match(text);
        if (!match.Success)
        {
            match = PsalmEnglishRegex.Match(text);
        }

        if (!match.Success || !int.TryParse(match.Groups["id"].Value, out int id))
        {
            return 0;
        }

        return id;
    }

    private static int TryMatchPsalmFromLink(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        Match match = PsalmLinkRegex.Match(text);
        if (!match.Success || !int.TryParse(match.Groups["id"].Value, out int id))
        {
            return 0;
        }

        return id;
    }
}
