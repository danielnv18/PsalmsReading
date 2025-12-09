using Microsoft.EntityFrameworkCore;
using PsalmsReading.Application.Interfaces;
using PsalmsReading.Domain.Entities;
using PsalmsReading.Infrastructure.Data;
using PsalmsReading.Infrastructure.Entities;

namespace PsalmsReading.Infrastructure.Repositories;

public sealed class PsalmRepository : IPsalmRepository
{
    private readonly PsalmsDbContext _dbContext;

    public PsalmRepository(PsalmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Psalm>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var psalms = await _dbContext.Psalms.AsNoTracking().ToListAsync(cancellationToken);

        var epigraphs = await _dbContext.PsalmEpigraphs
            .AsNoTracking()
            .Include(pe => pe.Epigraph)
            .GroupBy(e => e.PsalmId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(x => x.Epigraph!.Name).ToList(), cancellationToken);

        var themes = await _dbContext.PsalmThemes
            .AsNoTracking()
            .Include(pt => pt.Theme)
            .GroupBy(t => t.PsalmId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(x => x.Theme!.Name).ToList(),
                cancellationToken);

        return psalms
            .Select(p => new Psalm(
                p.Id,
                p.Title,
                p.TotalVerses,
                p.Type,
                epigraphs.GetValueOrDefault(p.Id, new List<string>()),
                themes.GetValueOrDefault(p.Id, new List<string>())))
            .ToList();
    }

    public async Task<Psalm?> GetByIdAsync(int psalmId, CancellationToken cancellationToken = default)
    {
        var psalm = await _dbContext.Psalms.AsNoTracking().FirstOrDefaultAsync(p => p.Id == psalmId, cancellationToken);
        if (psalm is null)
        {
            return null;
        }

        var epigraphs = await _dbContext.PsalmEpigraphs.AsNoTracking()
            .Where(e => e.PsalmId == psalmId)
            .Include(pe => pe.Epigraph)
            .Select(e => e.Epigraph!.Name)
            .ToListAsync(cancellationToken);

        var themes = await _dbContext.PsalmThemes.AsNoTracking()
            .Where(t => t.PsalmId == psalmId)
            .Include(pt => pt.Theme)
            .Select(t => t.Theme!.Name)
            .ToListAsync(cancellationToken);

        return new Psalm(psalm.Id, psalm.Title, psalm.TotalVerses, psalm.Type, epigraphs, themes);
    }

    public async Task AddRangeAsync(IEnumerable<Psalm> psalms, CancellationToken cancellationToken = default)
    {
        var list = psalms.ToList();
        if (list.Count == 0)
        {
            return;
        }

        await _dbContext.Psalms.AddRangeAsync(list, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var epigraphs = new List<PsalmEpigraph>();
        var themeJoins = new List<PsalmTheme>();

        var uniqueThemeNames = list.SelectMany(p => p.Themes)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingThemes = await _dbContext.Themes
            .Where(t => uniqueThemeNames.Contains(t.Name))
            .ToListAsync(cancellationToken);

        var existingNames = new HashSet<string>(existingThemes.Select(t => t.Name), StringComparer.OrdinalIgnoreCase);
        var newThemes = uniqueThemeNames
            .Where(name => !existingNames.Contains(name))
            .Select(name => new Theme { Name = name })
            .ToList();

        if (newThemes.Count > 0)
        {
            await _dbContext.Themes.AddRangeAsync(newThemes, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            existingThemes.AddRange(newThemes);
        }

        var themeLookup = existingThemes.ToDictionary(t => t.Name, t => t.Id, StringComparer.OrdinalIgnoreCase);

        var uniqueEpigraphNames = list.SelectMany(p => p.Epigraphs)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingEpigraphs = await _dbContext.Epigraphs
            .Where(e => uniqueEpigraphNames.Contains(e.Name))
            .ToListAsync(cancellationToken);

        var existingEpigraphNames = new HashSet<string>(existingEpigraphs.Select(e => e.Name), StringComparer.OrdinalIgnoreCase);
        var newEpigraphs = uniqueEpigraphNames
            .Where(name => !existingEpigraphNames.Contains(name))
            .Select(name => new Epigraph { Name = name })
            .ToList();

        if (newEpigraphs.Count > 0)
        {
            await _dbContext.Epigraphs.AddRangeAsync(newEpigraphs, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            existingEpigraphs.AddRange(newEpigraphs);
        }

        var epigraphLookup = existingEpigraphs.ToDictionary(e => e.Name, e => e.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var psalm in list)
        {
            foreach (var epigraphName in psalm.Epigraphs)
            {
                if (epigraphLookup.TryGetValue(epigraphName, out var epigraphId))
                {
                    epigraphs.Add(new PsalmEpigraph
                    {
                        PsalmId = psalm.Id,
                        EpigraphId = epigraphId
                    });
                }
            }

            foreach (var themeName in psalm.Themes)
            {
                if (themeLookup.TryGetValue(themeName, out var themeId))
                {
                    themeJoins.Add(new PsalmTheme
                    {
                        PsalmId = psalm.Id,
                        ThemeId = themeId
                    });
                }
            }
        }

        if (epigraphs.Count > 0)
        {
            await _dbContext.PsalmEpigraphs.AddRangeAsync(epigraphs, cancellationToken);
        }

        if (themeJoins.Count > 0)
        {
            await _dbContext.PsalmThemes.AddRangeAsync(themeJoins, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceAllAsync(IEnumerable<Psalm> psalms, CancellationToken cancellationToken = default)
    {
        var list = psalms.ToList();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await _dbContext.PsalmEpigraphs.ExecuteDeleteAsync(cancellationToken);
        await _dbContext.PsalmThemes.ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Psalms.ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Epigraphs.ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Themes.ExecuteDeleteAsync(cancellationToken);

        if (list.Count > 0)
        {
            await AddRangeAsync(list, cancellationToken);
        }
        else
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Psalms.AsNoTracking().AnyAsync(cancellationToken);
    }
}
