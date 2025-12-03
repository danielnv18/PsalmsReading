using Microsoft.EntityFrameworkCore;
using PsalmsReading.Api;
using PsalmsReading.Api.Contracts;
using PsalmsReading.Api.Json;
using PsalmsReading.Application.Interfaces;
using PsalmsReading.Application.Models;
using PsalmsReading.Infrastructure.Data;
using PsalmsReading.Infrastructure.Repositories;
using PsalmsReading.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5158", "https://localhost:7158");

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

await app.InitializeDatabaseAsync();

var api = app.MapGroup("/api");

api.MapGet("/psalms", async (IPsalmRepository repository, CancellationToken cancellationToken) =>
{
    var psalms = await repository.GetAllAsync(cancellationToken);
    return Results.Ok(psalms.Select(MapPsalm));
});

api.MapGet("/psalms/{id:int}", async (int id, IPsalmRepository repository, CancellationToken cancellationToken) =>
{
    var psalm = await repository.GetByIdAsync(id, cancellationToken);
    return psalm is null ? Results.NotFound() : Results.Ok(MapPsalm(psalm));
});

api.MapGet("/readings", async (DateOnly? from, DateOnly? to, IReadingRepository repository, CancellationToken cancellationToken) =>
{
    if (from.HasValue && to.HasValue)
    {
        var range = await repository.GetByDateRangeAsync(from.Value, to.Value, cancellationToken);
        return Results.Ok(range.Select(MapReading));
    }

    var all = await repository.GetAllAsync(cancellationToken);
    return Results.Ok(all.Select(MapReading));
});

api.MapPost("/readings", async (CreateReadingRequest request, IReadingRepository repository, CancellationToken cancellationToken) =>
{
    if (request.PsalmId <= 0 || request.DateRead == default)
    {
        return Results.BadRequest("PsalmId and DateRead are required.");
    }

    var record = new PsalmsReading.Domain.Entities.ReadingRecord(Guid.NewGuid(), request.PsalmId, request.DateRead);
    await repository.AddAsync(record, cancellationToken);
    return Results.Created($"/api/readings/{record.Id}", MapReading(record));
});

api.MapPost("/schedule", async (ScheduleRequest request, IReadingScheduler scheduler, IPlannedReadingRepository plannedRepository, CancellationToken cancellationToken) =>
{
    if (!IsValidMonths(request.Months))
    {
        return Results.BadRequest("Months must be one of 1, 2, 3, 6.");
    }

    var start = request.StartDate;
    var end = start.AddMonths(request.Months);

    var planned = await scheduler.GenerateScheduleAsync(start, request.Months, cancellationToken);

    await plannedRepository.ClearRangeAsync(start, end, cancellationToken);
    await plannedRepository.SavePlansAsync(planned, cancellationToken);

    return Results.Ok(planned.Select(MapPlannedReading));
});

api.MapPost("/schedule/ics", async (ScheduleRequest request, IReadingScheduler scheduler, IPlannedReadingRepository plannedRepository, ICalendarExporter exporter, CancellationToken cancellationToken) =>
{
    if (!IsValidMonths(request.Months))
    {
        return Results.BadRequest("Months must be one of 1, 2, 3, 6.");
    }

    var start = request.StartDate;
    var end = start.AddMonths(request.Months);

    var planned = await scheduler.GenerateScheduleAsync(start, request.Months, cancellationToken);

    await plannedRepository.ClearRangeAsync(start, end, cancellationToken);
    await plannedRepository.SavePlansAsync(planned, cancellationToken);

    var ics = await exporter.CreateCalendarAsync(planned, cancellationToken);
    if (string.IsNullOrWhiteSpace(ics))
    {
        return Results.NoContent();
    }

    return Results.Text(ics, "text/calendar");
});

static bool IsValidMonths(int months) => months is 1 or 2 or 3 or 6;

static PsalmDto MapPsalm(PsalmsReading.Domain.Entities.Psalm psalm) =>
    new(psalm.Id, psalm.Title, psalm.TotalVerses, psalm.Type, psalm.Epigraphs, psalm.Themes);

static ReadingRecordDto MapReading(PsalmsReading.Domain.Entities.ReadingRecord record) =>
    new(record.Id, record.PsalmId, record.DateRead);

static PlannedReadingDto MapPlannedReading(PsalmsReading.Domain.Entities.PlannedReading planned) =>
    new(planned.Id, planned.PsalmId, planned.ScheduledDate, planned.RuleApplied);

await app.RunAsync();
