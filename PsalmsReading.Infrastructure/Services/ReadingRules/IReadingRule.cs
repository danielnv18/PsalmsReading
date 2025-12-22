using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Services.ReadingRules;

public interface IReadingRule
{
    public string Name { get; }
    public bool CanApply(ScheduleContext context);
    public Psalm? Select(ScheduleContext context);
}
