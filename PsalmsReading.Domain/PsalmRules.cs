namespace PsalmsReading.Domain;

public static class PsalmRules
{
    private const int MaxReadableVerses = 30;

    /// <summary>
    /// Imprecatory psalms are those that invoke curses or divine judgment against enemies.
    /// These psalms are excluded from reading due to their themes of vengeance.
    /// </summary>
    private static readonly HashSet<int> ImprecatoryPsalmIds = [5, 10, 17, 28, 31, 35, 40, 52, 54, 55, 56, 58, 59, 69, 70, 71, 79, 83, 94, 109, 129, 137, 140, 143];

    public static bool IsReadable(Entities.Psalm psalm)
    {
        ArgumentNullException.ThrowIfNull(psalm);

        return psalm.TotalVerses <= MaxReadableVerses && !ImprecatoryPsalmIds.Contains(psalm.Id);
    }
}
