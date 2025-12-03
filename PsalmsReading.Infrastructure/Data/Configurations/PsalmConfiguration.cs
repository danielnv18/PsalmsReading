using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Data.Configurations;

public sealed class PsalmConfiguration : IEntityTypeConfiguration<Psalm>
{
    public void Configure(EntityTypeBuilder<Psalm> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Type).HasMaxLength(100);
        builder.Property(p => p.TotalVerses).IsRequired();
    }
}
