namespace PsalmsReading.Domain.Entities;

public sealed class ReadingRecord
{
    public Guid Id { get; }
    public int PsalmId { get; }
    public DateOnly DateRead { get; }

    public ReadingRecord(Guid id, int psalmId, DateOnly dateRead)
    {
        if (psalmId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(psalmId), "Psalm id must be positive.");
        }

        if (dateRead == default)
        {
            throw new ArgumentException("Date read is required.", nameof(dateRead));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        PsalmId = psalmId;
        DateRead = dateRead;
    }
}
