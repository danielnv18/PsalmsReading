using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Data.Configurations;

public sealed class PlannedReadingConfiguration : IEntityTypeConfiguration<PlannedReading>
{
    public void Configure(EntityTypeBuilder<PlannedReading> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PsalmId).IsRequired();
        builder.Property(p => p.ScheduledDate).IsRequired();
        builder.Property(p => p.RuleApplied).IsRequired().HasMaxLength(200);

        builder.HasIndex(p => new { p.ScheduledDate, p.PsalmId }).IsUnique();
    }
}
