using TUnit.Core;

namespace TUnit.Assertions.Tests;

public class RegexAPITests
{
    [Test]
    public async Task Test_Matches_WithGroup_DirectCall()
    {
        var email = "john.doe@example.com";
        var pattern = @"(?<username>[\w.]+)@(?<domain>[\w.]+)";

        // Test the new API - requires .And before .Group() for readability
        await Assert.That(email)
            .Matches(pattern)
            .And.Group("username", user => user.IsEqualTo("john.doe"))
            .And.Group("domain", domain => domain.IsEqualTo("example.com"));
    }

    [Test]
    public async Task Test_Matches_WithAnd_ThenGroup()
    {
        var email = "john.doe@example.com";
        var pattern = @"(?<username>[\w.]+)@(?<domain>[\w.]+)";

        // Test with .And before first .Group()
        await Assert.That(email)
            .Matches(pattern)
            .And.Group("username", user => user.IsEqualTo("john.doe"))
            .And.Group("domain", domain => domain.IsEqualTo("example.com"));
    }

    [Test]
    public async Task Test_Matches_WithMatchAt()
    {
        var text = "test123 hello456";
        var pattern = @"\w+\d+";

        // Test accessing multiple matches - requires .And before .Match()
        await Assert.That(text)
            .Matches(pattern)
            .And.Match(0)
            .And.Group(0, match => match.IsEqualTo("test123"));
    }

    [Test]
    public async Task Test_Matches_IndexedGroups()
    {
        var date = "2025-10-28";
        var pattern = @"(\d{4})-(\d{2})-(\d{2})";

        // Test indexed groups - all require .And for consistency
        await Assert.That(date)
            .Matches(pattern)
            .And.Group(0, full => full.IsEqualTo("2025-10-28"))
            .And.Group(1, year => year.IsEqualTo("2025"))
            .And.Group(2, month => month.IsEqualTo("10"))
            .And.Group(3, day => day.IsEqualTo("28"));
    }

    [Test]
    public async Task Test_Match_WithLambda()
    {
        var text = "test123 hello456";
        var pattern = @"\w+\d+";

        // Test lambda pattern for match assertions
        await Assert.That(text)
            .Matches(pattern)
            .And.Match(0, match => match.Group(0, g => g.IsEqualTo("test123")));
    }
}
