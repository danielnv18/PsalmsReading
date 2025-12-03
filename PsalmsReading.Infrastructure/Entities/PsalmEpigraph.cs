using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Entities;

public sealed class PsalmEpigraph
{
    public int Id { get; set; }
    public int PsalmId { get; set; }
    public string Name { get; set; } = string.Empty;

    public Psalm? Psalm { get; set; }
}
