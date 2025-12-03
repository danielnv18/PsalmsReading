using Microsoft.EntityFrameworkCore;
using PsalmsReading.Domain.Entities;
using PsalmsReading.Infrastructure.Entities;

namespace PsalmsReading.Infrastructure.Data;

public sealed class PsalmsDbContext : DbContext
{
    public PsalmsDbContext(DbContextOptions<PsalmsDbContext> options) : base(options)
    {
    }

    public DbSet<Psalm> Psalms => Set<Psalm>();
    public DbSet<ReadingRecord> ReadingRecords => Set<ReadingRecord>();
    public DbSet<PlannedReading> PlannedReadings => Set<PlannedReading>();
    public DbSet<PsalmEpigraph> PsalmEpigraphs => Set<PsalmEpigraph>();
    public DbSet<PsalmTheme> PsalmThemes => Set<PsalmTheme>();
    public DbSet<Theme> Themes => Set<Theme>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PsalmsDbContext).Assembly);
    }
}
