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

        var csvPath = FindCsv(app.Environment);
        if (csvPath is null)
        {
            app.Logger.LogWarning("Seed skipped: CSV file not found in content root or output folder.");
            return;
        }

        await using var stream = File.OpenRead(csvPath);
        await importService.ImportIfEmptyAsync(stream, cancellationToken);
    }

    internal static string? FindCsv(IHostEnvironment environment)
    {
        var candidates = new[]
        {
            Path.Combine(environment.ContentRootPath, "psalms_full_list.csv"),
            Path.Combine(AppContext.BaseDirectory, "psalms_full_list.csv")
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
