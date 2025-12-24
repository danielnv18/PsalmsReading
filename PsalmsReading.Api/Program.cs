using Microsoft.EntityFrameworkCore;
using PsalmsReading.Api;
using PsalmsReading.Api.Contracts;
using PsalmsReading.Api.Json;
using PsalmsReading.Application.Interfaces;
using PsalmsReading.Application.Models;
using PsalmsReading.Domain;
using PsalmsReading.Domain.Entities;
using PsalmsReading.Infrastructure.Data;
using PsalmsReading.Infrastructure.Repositories;
using PsalmsReading.Infrastructure.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5158", "https://localhost:7158");

var isReimportCommand = args.Any(a => string.Equals(a, "--reimport", StringComparison.OrdinalIgnoreCase));

builder.Services.AddDbContext<PsalmsDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlite(connectionString);
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new DateOnlyJsonConverter());
});

builder.Services.AddScoped<IPsalmRepository, PsalmRepository>();
builder.Services.AddScoped<IReadingRepository, ReadingRepository>();
builder.Services.AddScoped<IPsalmImportService, PsalmImportService>();
builder.Services.AddScoped<IReadingScheduler, ReadingScheduler>();
builder.Services.AddScoped<ICalendarExporter, CalendarExporter>();
builder.Services.AddScoped<IReadingExportService, ReadingExportService>();
builder.Services.AddScoped<IReadingImportService, ReadingImportService>();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

WebApplication app = builder.Build();

// CLI helper: dotnet run --project PsalmsReading.Api -- --reimport
if (isReimportCommand)
{
    using IServiceScope scope = app.Services.CreateScope();
    IServiceProvider services = scope.ServiceProvider;
    IPsalmImportService importService = services.GetRequiredService<IPsalmImportService>();
    IHostEnvironment environment = services.GetRequiredService<IHostEnvironment>();

    var csvPath = DatabaseInitializationExtensions.FindCsv(environment);
    if (csvPath is null)
    {
        Console.WriteLine("CSV file not found. Place psalms_full_list.csv next to the API project or in the output folder.");
        return;
    }

    await using FileStream stream = File.OpenRead(csvPath);
    await importService.ReimportAsync(stream);
    Console.WriteLine($"Psalms re-imported from: {csvPath}");
    return;
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

await app.InitializeDatabaseAsync();
app.UseCors();

RouteGroupBuilder api = app.MapGroup("/api");

api.MapGet("/psalms", async (IPsalmRepository repository, CancellationToken cancellationToken) =>
{
    IReadOnlyList<Psalm> psalms = await repository.GetAllAsync(cancellationToken);
    return Results.Ok(psalms.Select(MapPsalm));
});

api.MapGet("/psalms/{id:int}", async (int id, IPsalmRepository repository, CancellationToken cancellationToken) =>
{
    Psalm? psalm = await repository.GetByIdAsync(id, cancellationToken);
    return psalm is null ? Results.NotFound() : Results.Ok(MapPsalm(psalm));
});

api.MapPost("/psalms/reimport", async (IPsalmImportService importService, IHostEnvironment environment, CancellationToken cancellationToken) =>
{
    var csvPath = DatabaseInitializationExtensions.FindCsv(environment);
    if (csvPath is null)
    {
        return Results.NotFound("CSV file not found. Place psalms_full_list.csv next to the API project or in the output folder.");
    }

    await using FileStream stream = File.OpenRead(csvPath);
    await importService.ReimportAsync(stream, cancellationToken);

    return Results.Ok(new { message = "Psalms re-imported from CSV.", source = csvPath });
});

api.MapGet("/readings", async (
    DateOnly? from,
    DateOnly? to,
    IReadingRepository repository,
    CancellationToken cancellationToken) =>
{
    if (from.HasValue != to.HasValue)
    {
        return Results.BadRequest("Both from and to are required when filtering by date.");
    }

    var today = DateOnly.FromDateTime(DateTime.Today);
    DateOnly start = from ?? new DateOnly(today.Year, 1, 1);
    DateOnly end = to ?? new DateOnly(today.Year, 12, 31);

    if (start > end)
    {
        return Results.BadRequest("From must be earlier than or equal to to.");
    }

    IReadOnlyList<ReadingRecord> range = await repository.GetByDateRangeAsync(start, end, cancellationToken);
    return Results.Ok(range.Select(MapReading));
});

api.MapPost("/readings", async (CreateReadingRequest request, IReadingRepository repository, CancellationToken cancellationToken) =>
{
    if (request.PsalmId <= 0 || request.DateRead == default)
    {
        return Results.BadRequest("PsalmId and DateRead are required.");
    }

    IReadOnlyList<ReadingRecord> sameDay = await repository.GetByDateRangeAsync(request.DateRead, request.DateRead, cancellationToken);
    if (sameDay.Any(r => r.PsalmId == request.PsalmId))
    {
        return Results.Conflict("A reading for this psalm and date already exists.");
    }

    var record = new ReadingRecord(Guid.NewGuid(), request.PsalmId, request.DateRead);
    await repository.AddAsync(record, cancellationToken);
    return Results.Created($"/api/readings/{record.Id}", MapReading(record));
});

api.MapPut("/readings/{id:guid}", async (Guid id, UpdateReadingRequest request, IReadingRepository repository, CancellationToken cancellationToken) =>
{
    if (id == Guid.Empty || request.PsalmId <= 0 || request.DateRead == default)
    {
        return Results.BadRequest("Id, PsalmId and DateRead are required.");
    }

    IReadOnlyList<ReadingRecord> sameDay = await repository.GetByDateRangeAsync(request.DateRead, request.DateRead, cancellationToken);
    if (sameDay.Any(r => r.PsalmId == request.PsalmId && r.Id != id))
    {
        return Results.Conflict("A reading for this psalm and date already exists.");
    }

    var updated = new ReadingRecord(id, request.PsalmId, request.DateRead);
    var success = await repository.UpdateAsync(updated, cancellationToken);
    return success ? Results.Ok(MapReading(updated)) : Results.NotFound();
});

api.MapDelete("/readings/{id:guid}", async (Guid id, IReadingRepository repository, CancellationToken cancellationToken) =>
{
    if (id == Guid.Empty)
    {
        return Results.BadRequest("Id is required.");
    }

    var success = await repository.DeleteAsync(id, cancellationToken);
    return success ? Results.NoContent() : Results.NotFound();
});

api.MapPost("/readings/bulk-delete", async (
    BulkDeleteReadingsRequest request,
    IReadingRepository repository,
    CancellationToken cancellationToken) =>
{
    if (request.Dates.Count == 0)
    {
        return Results.BadRequest("Dates are required.");
    }

    var dates = request.Dates
        .Where(d => d != default)
        .Distinct()
        .ToList();

    if (dates.Count == 0)
    {
        return Results.BadRequest("Valid dates are required.");
    }

    var deletedCount = await repository.DeleteByDatesAsync(dates, cancellationToken);
    return Results.Ok(new { deletedCount });
});

api.MapGet("/readings/export/json", async (
    string? range,
    int? year,
    IReadingExportService exportService,
    CancellationToken cancellationToken) =>
{
    if (!TryParseReadingExportRange(range, out ReadingExportRange exportRange))
    {
        return Results.BadRequest("Range must be all or year.");
    }

    if (exportRange == ReadingExportRange.Year && !year.HasValue)
    {
        year = DateTime.Today.Year;
    }

    ReadingExportDto export = await exportService.ExportJsonAsync(exportRange, year, cancellationToken);
    return Results.Ok(export);
});

api.MapGet("/readings/export/ics", async (
    string? range,
    int? year,
    IReadingExportService exportService,
    ICalendarExporter calendarExporter,
    CancellationToken cancellationToken) =>
{
    if (!TryParseReadingExportRange(range, out ReadingExportRange exportRange))
    {
        return Results.BadRequest("Range must be all or year.");
    }

    if (exportRange == ReadingExportRange.Year && !year.HasValue)
    {
        year = DateTime.Today.Year;
    }

    IReadOnlyList<ReadingRecord> readings = await exportService.GetReadingsAsync(exportRange, year, cancellationToken);
    var planned = readings
        .Select(r => new PlannedReading(r.Id, r.PsalmId, r.DateRead, r.RuleApplied))
        .ToList();

    var ics = await calendarExporter.CreateCalendarAsync(planned, cancellationToken);
    return Results.Text(ics, "text/calendar");
});

api.MapPost("/readings/import/preview", async (
    ReadingExportDto importData,
    IReadingImportService importService,
    CancellationToken cancellationToken) =>
{
    try
    {
        ReadingImportPreviewDto preview = await importService.PreviewAsync(importData, cancellationToken);
        return Results.Ok(preview);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

api.MapPost("/readings/import", async (
    string? mode,
    ReadingExportDto importData,
    IReadingImportService importService,
    CancellationToken cancellationToken) =>
{
    if (!TryParseReadingImportMode(mode, out ReadingImportMode importMode))
    {
        return Results.BadRequest("Mode must be replace or ignore.");
    }

    try
    {
        ReadingImportResultDto result = await importService.ImportAsync(importData, importMode, cancellationToken);
        return Results.Ok(result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

api.MapGet("/stats", async (
    string? range,
    int? year,
    IPsalmRepository psalmRepository,
    IReadingRepository readingRepository,
    CancellationToken cancellationToken) =>
{
    if (!TryParseRange(range, out StatsRange rangeValue))
    {
        return Results.BadRequest("Range must be all, last6months, or year.");
    }

    DateOnly? rangeStart = null;
    DateOnly? rangeEnd = null;
    var today = DateOnly.FromDateTime(DateTime.Today);

    switch (rangeValue)
    {
        case StatsRange.Last6Months:
            rangeEnd = today;
            rangeStart = today.AddMonths(-6);
            break;
        case StatsRange.Year when !year.HasValue || year.Value < 1:
            return Results.BadRequest("Year is required when range=year.");
        case StatsRange.Year:
            rangeStart = new DateOnly(year.Value, 1, 1);
            rangeEnd = new DateOnly(year.Value, 12, 31);
            break;
        case StatsRange.All:
            break;
        default:
            throw new ArgumentOutOfRangeException();
    }

    IReadOnlyList<Psalm> psalms = await psalmRepository.GetAllAsync(cancellationToken);
    var readablePsalms = psalms.Where(PsalmRules.IsReadable).ToList();
    var readableById = readablePsalms.ToDictionary(p => p.Id);

    IReadOnlyList<ReadingRecord> readingsAll = await readingRepository.GetAllAsync(cancellationToken);
    var actualAll = readingsAll.Where(r => r.DateRead <= today).ToList();
    var plannedAll = readingsAll.Where(r => r.DateRead > today).ToList();

    List<ReadingRecord> readingsInRange = FilterReadings(actualAll, rangeStart, rangeEnd);
    List<ReadingRecord> plannedInRange = FilterReadings(plannedAll, rangeStart, rangeEnd);

    var readPsalmIds = actualAll
        .Where(r => readableById.ContainsKey(r.PsalmId))
        .Select(r => r.PsalmId)
        .ToHashSet();

    var plannedPsalmIds = plannedAll
        .Where(p => readableById.ContainsKey(p.PsalmId))
        .Select(p => p.PsalmId)
        .ToHashSet();

    HashSet<int> projectedPsalmIds = new(readPsalmIds);
    projectedPsalmIds.UnionWith(plannedPsalmIds);

    var totalReadable = readablePsalms.Count;
    var readablePsalmsRead = readPsalmIds.Count;
    var readablePsalmsProjected = projectedPsalmIds.Count;

    var actualReadsInRange = readingsInRange.Count(r => readableById.ContainsKey(r.PsalmId));
    var plannedReadsInRange = plannedInRange.Count(p => readableById.ContainsKey(p.PsalmId));

    List<TypeStatsDto> typeStats = BuildTypeStats(
        readablePsalms,
        readingsInRange,
        plannedInRange,
        readPsalmIds,
        projectedPsalmIds);

    StatsDto response = new(
        GetRangeLabel(rangeValue),
        rangeStart,
        rangeEnd,
        totalReadable,
        readablePsalmsRead,
        readablePsalmsProjected,
        actualReadsInRange,
        plannedReadsInRange,
        typeStats);

    return Results.Ok(response);
});

api.MapPost("/schedule", async (ScheduleRequest request, IReadingScheduler scheduler, IReadingRepository readingRepository, CancellationToken cancellationToken) =>
{
    if (!IsValidMonths(request.Months))
    {
        return Results.BadRequest("Months must be one of 1, 2, 3, 6, 12.");
    }

    DateOnly start = request.StartDate;
    DateOnly end = start.AddMonths(request.Months);
    var today = DateOnly.FromDateTime(DateTime.Today);

    IReadOnlyList<PlannedReading> planned = await scheduler.GenerateScheduleAsync(start, request.Months, cancellationToken);

    await readingRepository.ClearRangeAsync(start, end, today.AddDays(1), cancellationToken);
    IReadOnlyList<ReadingRecord> toSave = planned.Select(MapPlannedToRecord).ToList();
    IReadOnlyList<ReadingRecord> existing = await readingRepository.GetByDateRangeAsync(start, end, cancellationToken);
    var existingKeys = existing
        .Select(r => (r.PsalmId, r.DateRead))
        .ToHashSet();
    var newRecords = toSave
        .Where(r => !existingKeys.Contains((r.PsalmId, r.DateRead)))
        .ToList();
    await readingRepository.AddRangeAsync(newRecords, cancellationToken);

    return Results.Ok(planned.Select(MapPlannedReading));
});

api.MapPost("/schedule/preview", async (ScheduleRequest request, IReadingScheduler scheduler, CancellationToken cancellationToken) =>
{
    if (!IsValidMonths(request.Months))
    {
        return Results.BadRequest("Months must be one of 1, 2, 3, 6, 12.");
    }

    IReadOnlyList<PlannedReading> planned = await scheduler.GenerateScheduleAsync(request.StartDate, request.Months, cancellationToken);
    return Results.Ok(planned.Select(MapPlannedReading));
});

api.MapPost("/schedule/ics", async (ScheduleRequest request, IReadingScheduler scheduler, IReadingRepository readingRepository, ICalendarExporter exporter, CancellationToken cancellationToken) =>
{
    if (!IsValidMonths(request.Months))
    {
        return Results.BadRequest("Months must be one of 1, 2, 3, 6, 12.");
    }

    DateOnly start = request.StartDate;
    DateOnly end = start.AddMonths(request.Months);
    var today = DateOnly.FromDateTime(DateTime.Today);

    IReadOnlyList<PlannedReading> planned = await scheduler.GenerateScheduleAsync(start, request.Months, cancellationToken);

    await readingRepository.ClearRangeAsync(start, end, today.AddDays(1), cancellationToken);
    IReadOnlyList<ReadingRecord> toSave = planned.Select(MapPlannedToRecord).ToList();
    IReadOnlyList<ReadingRecord> existing = await readingRepository.GetByDateRangeAsync(start, end, cancellationToken);
    var existingKeys = existing
        .Select(r => (r.PsalmId, r.DateRead))
        .ToHashSet();
    var newRecords = toSave
        .Where(r => !existingKeys.Contains((r.PsalmId, r.DateRead)))
        .ToList();
    await readingRepository.AddRangeAsync(newRecords, cancellationToken);

    var ics = await exporter.CreateCalendarAsync(planned, cancellationToken);
    return string.IsNullOrWhiteSpace(ics) ? Results.NoContent() : Results.Text(ics, "text/calendar");
});

static bool IsValidMonths(int months) => months is 1 or 2 or 3 or 6 or 12;

static PsalmDto MapPsalm(Psalm psalm) =>
    new(psalm.Id, psalm.Title, psalm.TotalVerses, psalm.Type, psalm.Epigraphs, psalm.Themes);

static ReadingRecordDto MapReading(ReadingRecord record) =>
    new(record.Id, record.PsalmId, record.DateRead, record.RuleApplied);

static PlannedReadingDto MapPlannedReading(PlannedReading planned) =>
    new(planned.Id, planned.PsalmId, planned.ScheduledDate, planned.RuleApplied);

static ReadingRecord MapPlannedToRecord(PlannedReading planned) =>
    new(planned.Id, planned.PsalmId, planned.ScheduledDate, planned.RuleApplied);

static string GetRangeLabel(StatsRange range) =>
    range switch
    {
        StatsRange.All => "all",
        StatsRange.Last6Months => "last6months",
        StatsRange.Year => "year",
        _ => "all"
    };

static List<ReadingRecord> FilterReadings(IReadOnlyList<ReadingRecord> readings, DateOnly? start, DateOnly? end)
{
    IEnumerable<ReadingRecord> filtered = readings;
    if (start.HasValue && end.HasValue)
    {
        filtered = filtered.Where(r => r.DateRead >= start.Value && r.DateRead <= end.Value);
    }

    return filtered.ToList();
}

static List<TypeStatsDto> BuildTypeStats(
    IReadOnlyList<Psalm> readablePsalms,
    IReadOnlyList<ReadingRecord> readingsInRange,
    IReadOnlyList<ReadingRecord> plannedInRange,
    HashSet<int> readPsalmIds,
    HashSet<int> projectedPsalmIds)
{
    IEnumerable<IGrouping<string, Psalm>> grouped = readablePsalms
        .GroupBy(p => NormalizeType(p.Type), StringComparer.OrdinalIgnoreCase)
        .OrderBy(g => g.Key);

    return (from @group in grouped let idsInType = @group.Select(p => p.Id).ToHashSet() let totalReadable = idsInType.Count let actualReads = readingsInRange.Count(r => idsInType.Contains(r.PsalmId)) let plannedReads = plannedInRange.Count(p => idsInType.Contains(p.PsalmId)) let actualCoverage = idsInType.Count(readPsalmIds.Contains) let projectedCoverage = idsInType.Count(projectedPsalmIds.Contains) select new TypeStatsDto(@group.Key, totalReadable, actualReads, plannedReads, actualCoverage, projectedCoverage)).ToList();
}

static bool TryParseReadingExportRange(string? range, out ReadingExportRange exportRange)
{
    if (string.IsNullOrWhiteSpace(range) || string.Equals(range, "year", StringComparison.OrdinalIgnoreCase))
    {
        exportRange = ReadingExportRange.Year;
        return true;
    }

    if (string.Equals(range, "all", StringComparison.OrdinalIgnoreCase))
    {
        exportRange = ReadingExportRange.All;
        return true;
    }

    exportRange = ReadingExportRange.Year;
    return false;
}

static bool TryParseReadingImportMode(string? mode, out ReadingImportMode importMode)
{
    if (string.Equals(mode, "replace", StringComparison.OrdinalIgnoreCase))
    {
        importMode = ReadingImportMode.ReplaceConflicts;
        return true;
    }

    if (string.Equals(mode, "ignore", StringComparison.OrdinalIgnoreCase))
    {
        importMode = ReadingImportMode.IgnoreConflicts;
        return true;
    }

    importMode = ReadingImportMode.IgnoreConflicts;
    return false;
}

static string NormalizeType(string? type) =>
    string.IsNullOrWhiteSpace(type) ? "Sin tipo" : type.Trim();

await app.RunAsync();
return;

static bool TryParseRange(string? range, out StatsRange parsed)
{
    var value = string.IsNullOrWhiteSpace(range) ? "all" : range.Trim().ToLowerInvariant();
    parsed = value switch
    {
        "all" => StatsRange.All,
        "last6months" => StatsRange.Last6Months,
        "year" => StatsRange.Year,
        _ => StatsRange.All
    };

    return value is "all" or "last6months" or "year";
}

public partial class Program
{
}

enum StatsRange
{
    All,
    Last6Months,
    Year
}
