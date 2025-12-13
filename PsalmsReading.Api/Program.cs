using Microsoft.EntityFrameworkCore;
using PsalmsReading.Api;
using PsalmsReading.Api.Contracts;
using PsalmsReading.Api.Json;
using PsalmsReading.Application.Interfaces;
using PsalmsReading.Application.Models;
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
builder.Services.AddScoped<IPlannedReadingRepository, PlannedReadingRepository>();
builder.Services.AddScoped<IPsalmImportService, PsalmImportService>();
builder.Services.AddScoped<IReadingScheduler, ReadingScheduler>();
builder.Services.AddScoped<ICalendarExporter, CalendarExporter>();

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

api.MapGet("/readings", async (DateOnly? from, DateOnly? to, IReadingRepository repository, CancellationToken cancellationToken) =>
{
    if (from.HasValue && to.HasValue)
    {
        IReadOnlyList<ReadingRecord> range = await repository.GetByDateRangeAsync(from.Value, to.Value, cancellationToken);
        return Results.Ok(range.Select(MapReading));
    }

    IReadOnlyList<ReadingRecord> all = await repository.GetAllAsync(cancellationToken);
    return Results.Ok(all.Select(MapReading));
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

    var record = new PsalmsReading.Domain.Entities.ReadingRecord(Guid.NewGuid(), request.PsalmId, request.DateRead);
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

    var updated = new PsalmsReading.Domain.Entities.ReadingRecord(id, request.PsalmId, request.DateRead);
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

api.MapPost("/schedule", async (ScheduleRequest request, IReadingScheduler scheduler, IPlannedReadingRepository plannedRepository, CancellationToken cancellationToken) =>
{
    if (!IsValidMonths(request.Months))
    {
        return Results.BadRequest("Months must be one of 1, 2, 3, 6, 12.");
    }

    DateOnly start = request.StartDate;
    DateOnly end = start.AddMonths(request.Months);

    IReadOnlyList<PlannedReading> planned = await scheduler.GenerateScheduleAsync(start, request.Months, cancellationToken);

    await plannedRepository.ClearRangeAsync(start, end, cancellationToken);
    await plannedRepository.SavePlansAsync(planned, cancellationToken);

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

api.MapPost("/schedule/ics", async (ScheduleRequest request, IReadingScheduler scheduler, IPlannedReadingRepository plannedRepository, ICalendarExporter exporter, CancellationToken cancellationToken) =>
{
    if (!IsValidMonths(request.Months))
    {
        return Results.BadRequest("Months must be one of 1, 2, 3, 6, 12.");
    }

    DateOnly start = request.StartDate;
    DateOnly end = start.AddMonths(request.Months);

    IReadOnlyList<PlannedReading> planned = await scheduler.GenerateScheduleAsync(start, request.Months, cancellationToken);

    await plannedRepository.ClearRangeAsync(start, end, cancellationToken);
    await plannedRepository.SavePlansAsync(planned, cancellationToken);

    var ics = await exporter.CreateCalendarAsync(planned, cancellationToken);
    if (string.IsNullOrWhiteSpace(ics))
    {
        return Results.NoContent();
    }

    return Results.Text(ics, "text/calendar");
});

static bool IsValidMonths(int months) => months is 1 or 2 or 3 or 6 or 12;

static PsalmDto MapPsalm(PsalmsReading.Domain.Entities.Psalm psalm) =>
    new(psalm.Id, psalm.Title, psalm.TotalVerses, psalm.Type, psalm.Epigraphs, psalm.Themes);

static ReadingRecordDto MapReading(PsalmsReading.Domain.Entities.ReadingRecord record) =>
    new(record.Id, record.PsalmId, record.DateRead);

static PlannedReadingDto MapPlannedReading(PsalmsReading.Domain.Entities.PlannedReading planned) =>
    new(planned.Id, planned.PsalmId, planned.ScheduledDate, planned.RuleApplied);

await app.RunAsync();

public partial class Program
{
}
