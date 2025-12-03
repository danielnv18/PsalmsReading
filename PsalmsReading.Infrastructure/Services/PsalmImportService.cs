using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using PsalmsReading.Application.Interfaces;
using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Services;

public sealed class PsalmImportService : IPsalmImportService
{
    private readonly IPsalmRepository _psalmRepository;

    public PsalmImportService(IPsalmRepository psalmRepository)
    {
        _psalmRepository = psalmRepository;
    }

    public async Task ImportIfEmptyAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        if (await _psalmRepository.AnyAsync(cancellationToken))
        {
            return;
        }

        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ",",
            BadDataFound = null,
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim,
        });

        var records = new List<Psalm>();
        await foreach (var row in csv.GetRecordsAsync<PsalmCsvRow>(cancellationToken))
        {
            var epigraphs = SplitList(row.Epigrafes);
            var themes = SplitList(row.Temas);

            var psalm = new Psalm(
                id: row.Capitulo,
                title: row.Titulo,
                totalVerses: row.Total_Verses,
                type: row.Tipo,
                epigraphs: epigraphs,
                themes: themes);

            records.Add(psalm);
        }

        await _psalmRepository.AddRangeAsync(records, cancellationToken);
    }

    private static IReadOnlyList<string> SplitList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed class PsalmCsvRow
    {
        public int Capitulo { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public int Total_Verses { get; set; }
        public string? Tipo { get; set; }
        public string? Epigrafes { get; set; }
        public string? Temas { get; set; }
    }
}
