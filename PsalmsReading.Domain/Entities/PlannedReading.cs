namespace PsalmsReading.Domain.Entities;

public sealed class PlannedReading
{
    public Guid Id { get; }
    public int PsalmId { get; }
    public DateOnly ScheduledDate { get; }
    public string? RuleApplied { get; }

    public PlannedReading(Guid id, int psalmId, DateOnly scheduledDate, string? ruleApplied)
    {
        if (psalmId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(psalmId), "Psalm id must be positive.");
        }

        if (scheduledDate == default)
        {
            throw new ArgumentException("Scheduled date is required.", nameof(scheduledDate));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        PsalmId = psalmId;
        ScheduledDate = scheduledDate;
        RuleApplied = string.IsNullOrWhiteSpace(ruleApplied) ? null : ruleApplied.Trim();
    }
}
