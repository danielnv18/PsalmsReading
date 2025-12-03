using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsalmsReading.Infrastructure.Entities;

namespace PsalmsReading.Infrastructure.Data.Configurations;

public sealed class PsalmThemeConfiguration : IEntityTypeConfiguration<PsalmTheme>
{
    public void Configure(EntityTypeBuilder<PsalmTheme> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.PsalmId, e.ThemeId }).IsUnique();

        builder.HasOne(e => e.Psalm)
            .WithMany()
            .HasForeignKey(e => e.PsalmId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Theme)
            .WithMany(t => t.PsalmThemes)
            .HasForeignKey(e => e.ThemeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
