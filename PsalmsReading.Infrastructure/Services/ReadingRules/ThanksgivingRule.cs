using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Services.ReadingRules;

public sealed class ThanksgivingRule : IReadingRule
{
    public string Name => "Thanksgiving";

    public bool CanApply(ScheduleContext context) => context.IsThanksgivingSunday;

    public Psalm? Select(ScheduleContext context) =>
        ReadingRuleHelpers.SelectByTheme(context.AvailablePsalms, context.ReadCounts, "Días festivos: Agradecimiento", context.Random);
}
