using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Services.ReadingRules;

public sealed class DecemberRule : IReadingRule
{
    public string Name => "Christmas season";

    public bool CanApply(ScheduleContext context) => context.IsDecember;

    public Psalm? Select(ScheduleContext context) =>
        ReadingRuleHelpers.SelectByTypeThemeOrEpigraph(context.AvailablePsalms, context.ReadCounts, "mesiánico", context.Random);
}
