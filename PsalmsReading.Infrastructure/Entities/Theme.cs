namespace PsalmsReading.Infrastructure.Entities;

public sealed class Theme
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<PsalmTheme> PsalmThemes { get; set; } = new List<PsalmTheme>();
}
