using PsalmsReading.Domain.Entities;
using Xunit;

namespace PsalmsReading.Tests;

public class DomainTests
{
    [Fact]
    public void Psalm_TracksThemesCaseInsensitive()
    {
        var psalm = new Psalm(1, "Test", 10, "alabanza", null, new[] { "Fe", "fe", "Gozo" });

        Assert.Equal(2, psalm.Themes.Count);
        Assert.True(psalm.HasTheme("FE"));
    }

    [Fact]
    public void Psalm_IsShortReadingCandidate_RespectsMaxVerses()
    {
        var shortPsalm = new Psalm(2, "Short", 30, "alabanza", null, null);
        var longPsalm = new Psalm(3, "Long", 31, "alabanza", null, null);

        Assert.True(shortPsalm.IsShortReadingCandidate());
        Assert.False(longPsalm.IsShortReadingCandidate());
    }

    [Fact]
    public void Psalm_IsExcluded_WhenInSet()
    {
        var excluded = new HashSet<int> { 35, 55 };
        var psalm = new Psalm(35, "Excluded", 10, null, null, null);

        Assert.True(psalm.IsExcluded(excluded));
    }

    [Fact]
    public void Psalm_HasType_IsCaseInsensitive()
    {
        var psalm = new Psalm(4, "Title", 5, "Alabanza", null, null);
        Assert.True(psalm.HasType("alabanza"));
    }

    [Fact]
    public void ReadingRecord_RequiresValidData()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReadingRecord(Guid.NewGuid(), 0, new DateOnly(2024, 1, 1)));
        Assert.Throws<ArgumentException>(() => new ReadingRecord(Guid.NewGuid(), 1, default));
    }

    [Fact]
    public void PlannedReading_RequiresRule()
    {
        Assert.Throws<ArgumentException>(() => new PlannedReading(Guid.NewGuid(), 1, new DateOnly(2024, 1, 1), ""));
    }
}
