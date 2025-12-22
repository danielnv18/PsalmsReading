using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Services.ReadingRules;

public sealed class HolyWeekRule(HashSet<int> preferredIds) : IReadingRule
{
    public string Name => "HolyWeek";

    public bool CanApply(ScheduleContext context) => context.IsHolyWeek;

    public Psalm? Select(ScheduleContext context) =>
        ReadingRuleHelpers.SelectBestByTier(
            context.AvailablePsalms.Where(p => preferredIds.Contains(p.Id)),
            context.ReadCounts,
            context.Random);
}
