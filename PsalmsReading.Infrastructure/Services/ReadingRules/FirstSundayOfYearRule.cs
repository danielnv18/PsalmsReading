using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Services.ReadingRules;

public sealed class FirstSundayOfYearRule : IReadingRule
{
    public string Name => "First Sunday new year";

    public bool CanApply(ScheduleContext context) => context.IsFirstSundayOfYear;

    public Psalm? Select(ScheduleContext context) =>
        ReadingRuleHelpers.SelectByTheme(context.AvailablePsalms, context.ReadCounts, "Días festivos: año nuevo", context.Random);
}
