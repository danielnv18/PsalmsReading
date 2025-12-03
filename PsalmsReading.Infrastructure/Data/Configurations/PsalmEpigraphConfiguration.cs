using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsalmsReading.Infrastructure.Entities;

namespace PsalmsReading.Infrastructure.Data.Configurations;

public sealed class PsalmEpigraphConfiguration : IEntityTypeConfiguration<PsalmEpigraph>
{
    public void Configure(EntityTypeBuilder<PsalmEpigraph> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(e => new { e.PsalmId, e.Name }).IsUnique();

        builder.HasOne(e => e.Psalm)
            .WithMany()
            .HasForeignKey(e => e.PsalmId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
