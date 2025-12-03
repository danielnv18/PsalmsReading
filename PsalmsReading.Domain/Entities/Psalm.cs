using System.Collections.ObjectModel;

namespace PsalmsReading.Domain.Entities;

public sealed class Psalm
{
    public int Id { get; }
    public string Title { get; }
    public int TotalVerses { get; }
    public string? Type { get; }
    public string? Epigraphs { get; }
    public IReadOnlyList<string> Themes { get; }

    public Psalm(int id, string title, int totalVerses, string? type, string? epigraphs, IEnumerable<string>? themes)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Psalm id must be positive.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (totalVerses <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalVerses), "Total verses must be positive.");
        }

        Id = id;
        Title = title.Trim();
        TotalVerses = totalVerses;
        Type = string.IsNullOrWhiteSpace(type) ? null : type.Trim();
        Epigraphs = string.IsNullOrWhiteSpace(epigraphs) ? null : epigraphs.Trim();

        var themeList = themes?
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        Themes = new ReadOnlyCollection<string>(themeList);
    }

    public bool IsShortReadingCandidate(int maxVerses = 30) => TotalVerses <= maxVerses;

    public bool HasType(string type) =>
        !string.IsNullOrWhiteSpace(Type) &&
        string.Equals(Type, type, StringComparison.OrdinalIgnoreCase);

    public bool HasTheme(string theme) =>
        Themes.Any(t => string.Equals(t, theme, StringComparison.OrdinalIgnoreCase));

    public bool IsExcluded(IReadOnlySet<int> excludedPsalmIds) =>
        excludedPsalmIds.Contains(Id);
}
