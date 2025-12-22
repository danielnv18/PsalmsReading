using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Services.ReadingRules;

public sealed class FirstSundayPraiseRule : IReadingRule
{
    public string Name => "First Sunday of worship";

    public bool CanApply(ScheduleContext context) => context.IsFirstSundayOfMonth;

    public Psalm? Select(ScheduleContext context) =>
        ReadingRuleHelpers.SelectByTypeThemeOrEpigraph(context.AvailablePsalms, context.ReadCounts, "alabanza", context.Random);
}
