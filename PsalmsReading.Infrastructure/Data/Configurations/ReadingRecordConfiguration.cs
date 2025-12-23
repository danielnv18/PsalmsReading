using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Data.Configurations;

public sealed class ReadingRecordConfiguration : IEntityTypeConfiguration<ReadingRecord>
{
    public void Configure(EntityTypeBuilder<ReadingRecord> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.PsalmId).IsRequired();
        builder.Property(r => r.DateRead).IsRequired();
        builder.Property(r => r.RuleApplied).HasMaxLength(200);

        builder.HasIndex(r => new { r.PsalmId, r.DateRead }).IsUnique();
    }
}
