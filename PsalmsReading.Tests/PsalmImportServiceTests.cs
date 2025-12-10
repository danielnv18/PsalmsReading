using System.Text;
using PsalmsReading.Application.Interfaces;
using PsalmsReading.Domain.Entities;
using PsalmsReading.Infrastructure.Services;

namespace PsalmsReading.Tests;

public class PsalmImportServiceTests
{
    [Fact]
    public async Task ImportIfEmpty_LoadsPsalms_AndSplitsLists()
    {
        using var csv = BuildCsvStream();
        var repo = new FakePsalmRepository(hasData: false);
        var service = new PsalmImportService(repo);

        await service.ImportIfEmptyAsync(csv);

        Assert.Equal(2, repo.Stored.Count);
        var psalm1 = repo.Stored.Single(p => p.Id == 1);
        Assert.Contains("selah", psalm1.Epigraphs, StringComparer.OrdinalIgnoreCase);
        Assert.Single(psalm1.Themes); // duplicates trimmed case-insensitively
    }

    [Fact]
    public async Task ImportIfEmpty_Skips_WhenDataExists()
    {
        using var csv = BuildCsvStream();
        var repo = new FakePsalmRepository(hasData: true);
        var service = new PsalmImportService(repo);

        await service.ImportIfEmptyAsync(csv);

        Assert.Empty(repo.Stored);
    }

    [Fact]
    public async Task Reimport_Always_Replaces_All()
    {
        using var csv = BuildCsvStream();
        var repo = new FakePsalmRepository(hasData: true);
        var service = new PsalmImportService(repo);

        await service.ReimportAsync(csv);

        Assert.Equal(2, repo.Stored.Count);
        Assert.True(repo.ReplaceCalled);
    }

    [Fact]
    public async Task CalendarExporter_Uses_Template()
    {
        var repo = new FakePsalmRepository(hasData: true);
        repo.Stored.Add(new Psalm(27, "Mi luz", 14, "alabanza", null, new[] { "adoración" }));
        var exporter = new CalendarExporter(repo);
        var plan = new PlannedReading(Guid.NewGuid(), 27, new DateOnly(2025, 1, 5), "General");

        var ics = await exporter.CreateCalendarAsync(new[] { plan });

        Assert.Contains("Salmo 27 - Mi luz", ics);
        Assert.Contains("Categoria: alabanza", ics);
        Assert.Contains("PSA.27.NBLA", ics);
    }

    private static MemoryStream BuildCsvStream()
    {
        const string csv = """
        capitulo,titulo,total_verses,tipo,epigrafes,temas
        1,Primero,10,alabanza,"Selah, selah","Fe, fe"
        2,Segundo,12,lamento,,"Confianza,Gozo"
        """;

        var bytes = Encoding.UTF8.GetBytes(csv);
        return new MemoryStream(bytes);
    }

    private sealed class FakePsalmRepository : IPsalmRepository
    {
        public List<Psalm> Stored { get; } = new();
        private readonly bool _hasDataOnStart;
        public bool ReplaceCalled { get; private set; }

        public FakePsalmRepository(bool hasData)
        {
            _hasDataOnStart = hasData;
        }

        public Task AddRangeAsync(IEnumerable<Psalm> psalms, CancellationToken cancellationToken = default)
        {
            Stored.AddRange(psalms);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Psalm>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Psalm>>(Stored);

        public Task<Psalm?> GetByIdAsync(int psalmId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored.FirstOrDefault(p => p.Id == psalmId));

        public Task ReplaceAllAsync(IEnumerable<Psalm> psalms, CancellationToken cancellationToken = default)
        {
            ReplaceCalled = true;
            Stored.Clear();
            Stored.AddRange(psalms);
            return Task.CompletedTask;
        }

        public Task<bool> AnyAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_hasDataOnStart);
    }
}
