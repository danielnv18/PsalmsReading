using Microsoft.EntityFrameworkCore;
using PsalmsReading.Application.Interfaces;
using PsalmsReading.Infrastructure.Data;

namespace PsalmsReading.Api;

internal static class DatabaseInitializationExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PsalmsDbContext>();
        var importService = scope.ServiceProvider.GetRequiredService<IPsalmImportService>();

        await context.Database.MigrateAsync(cancellationToken);

        var csvPath = Path.Combine(app.Environment.ContentRootPath, "psalms_full_list.csv");
        if (!File.Exists(csvPath))
        {
            app.Logger.LogWarning("Seed skipped: CSV file not found at {CsvPath}", csvPath);
            return;
        }

        await using var stream = File.OpenRead(csvPath);
        await importService.ImportIfEmptyAsync(stream, cancellationToken);
    }
}
