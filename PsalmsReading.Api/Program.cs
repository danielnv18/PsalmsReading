using Microsoft.EntityFrameworkCore;
using PsalmsReading.Api;
using PsalmsReading.Application.Interfaces;
using PsalmsReading.Infrastructure.Data;
using PsalmsReading.Infrastructure.Repositories;
using PsalmsReading.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PsalmsDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<IPsalmRepository, PsalmRepository>();
builder.Services.AddScoped<IReadingRepository, ReadingRepository>();
builder.Services.AddScoped<IPlannedReadingRepository, PlannedReadingRepository>();
builder.Services.AddScoped<IPsalmImportService, PsalmImportService>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await app.InitializeDatabaseAsync();

app.UseHttpsRedirection();

await app.RunAsync();
