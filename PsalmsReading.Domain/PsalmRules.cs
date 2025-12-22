namespace PsalmsReading.Domain;

public static class PsalmRules
{
    private const int MaxReadableVerses = 30;
    private static readonly IReadOnlySet<int> ExcludedPsalmIds = new HashSet<int> { 35, 55, 59, 69, 79, 109, 137 };

    public static bool IsReadable(Entities.Psalm psalm)
    {
        ArgumentNullException.ThrowIfNull(psalm);

        return psalm.TotalVerses <= MaxReadableVerses && !ExcludedPsalmIds.Contains(psalm.Id);
    }
}
