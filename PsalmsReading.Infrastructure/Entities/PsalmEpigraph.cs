using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Entities;

public sealed class PsalmEpigraph
{
    public int Id { get; set; }
    public int PsalmId { get; set; }
    public int EpigraphId { get; set; }

    public Psalm? Psalm { get; set; }
    public Epigraph? Epigraph { get; set; }
}
