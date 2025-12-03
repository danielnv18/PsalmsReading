using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Entities;

public sealed class PsalmTheme
{
    public int Id { get; set; }
    public int PsalmId { get; set; }
    public int ThemeId { get; set; }

    public Psalm? Psalm { get; set; }
    public Theme? Theme { get; set; }
}
