using TUnit.Assertions.Assertions.Regex;
using TUnit.Core;

namespace TUnit.Assertions.Tests;

public class RegexMatchResultTests
{
    [Test]
    public async Task Test_GetMatchAsync_NamedGroups()
    {
        // Arrange
        var email = "john.doe@example.com";
        var pattern = @"(?<username>[\w.]+)@(?<domain>[\w.]+)";

        // Act
        var match = await Assert.That(email).Matches(pattern).GetMatchAsync();

        // Assert
        await Assert.That(match.Group("username")).IsEqualTo("john.doe");
        await Assert.That(match.Group("domain")).IsEqualTo("example.com");
    }

    [Test]
    public async Task Test_GetMatchAsync_IndexedGroups()
    {
        // Arrange
        var date = "2025-10-28";
        var pattern = @"(\d{4})-(\d{2})-(\d{2})";

        // Act
        var match = await Assert.That(date).Matches(pattern).GetMatchAsync();

        // Assert
        await Assert.That(match.Group(0)).IsEqualTo("2025-10-28");
        await Assert.That(match.Group(1)).IsEqualTo("2025");
        await Assert.That(match.Group(2)).IsEqualTo("10");
        await Assert.That(match.Group(3)).IsEqualTo("28");
    }

    [Test]
    public async Task Test_GetMatchAsync_MatchProperties()
    {
        // Arrange
        var text = "Hello World 123";
        var pattern = @"\d+";

        // Act
        var match = await Assert.That(text).Matches(pattern).GetMatchAsync();

        // Assert
        await Assert.That(match.Value).IsEqualTo("123");
        await Assert.That(match.Index).IsEqualTo(12);
        await Assert.That(match.Length).IsEqualTo(3);
    }

    [Test]
    public async Task Test_GetMatchAsync_WithIgnoringCase()
    {
        // Arrange
        var text = "HELLO world";
        var pattern = @"hello";

        // Act
        var match = await Assert.That(text).Matches(pattern).IgnoringCase().GetMatchAsync();

        // Assert
        await Assert.That(match.Value).IsEqualTo("HELLO");
    }

    [Test]
    public async Task Test_RegexMatchResult_InvalidGroupName_ThrowsArgumentException()
    {
        // Arrange
        var text = "test@example.com";
        var pattern = @"(?<user>\w+)@(?<domain>[\w.]+)";
        var match = await Assert.That(text).Matches(pattern).GetMatchAsync();

        // Act & Assert
        await Assert.That(() => match.Group("")).Throws<ArgumentException>();
    }

    [Test]
    public async Task Test_RegexMatchResult_InvalidGroupIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var text = "test";
        var pattern = @"(\w+)";
        var match = await Assert.That(text).Matches(pattern).GetMatchAsync();

        // Act & Assert - accessing group index 5 when only 0 and 1 exist
        await Assert.That(() => match.Group(5)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Test_RegexMatchResult_NegativeGroupIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var text = "test";
        var pattern = @"(\w+)";
        var match = await Assert.That(text).Matches(pattern).GetMatchAsync();

        // Act & Assert
        await Assert.That(() => match.Group(-1)).Throws<ArgumentOutOfRangeException>();
    }
}
