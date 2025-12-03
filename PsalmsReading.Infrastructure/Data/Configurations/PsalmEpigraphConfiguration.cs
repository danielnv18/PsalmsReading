using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsalmsReading.Infrastructure.Entities;

namespace PsalmsReading.Infrastructure.Data.Configurations;

public sealed class PsalmEpigraphConfiguration : IEntityTypeConfiguration<PsalmEpigraph>
{
    public void Configure(EntityTypeBuilder<PsalmEpigraph> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.PsalmId, e.EpigraphId }).IsUnique();

        builder.HasOne(e => e.Psalm)
            .WithMany()
            .HasForeignKey(e => e.PsalmId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Epigraph)
            .WithMany(t => t.PsalmEpigraphs)
            .HasForeignKey(e => e.EpigraphId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
