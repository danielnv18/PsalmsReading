using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsalmsReading.Infrastructure.Entities;

namespace PsalmsReading.Infrastructure.Data.Configurations;

public sealed class EpigraphConfiguration : IEntityTypeConfiguration<Epigraph>
{
    public void Configure(EntityTypeBuilder<Epigraph> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Name).IsUnique();
    }
}
