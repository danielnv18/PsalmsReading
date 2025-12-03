namespace PsalmsReading.Infrastructure.Entities;

public sealed class Epigraph
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<PsalmEpigraph> PsalmEpigraphs { get; set; } = new List<PsalmEpigraph>();
}
