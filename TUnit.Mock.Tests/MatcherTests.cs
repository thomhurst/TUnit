using TUnit.Mock;
using TUnit.Mock.Arguments;

namespace TUnit.Mock.Tests;

/// <summary>
/// Phase 24 Tests: Additional argument matchers (InRange, IsIn, IsNotIn, Not).
/// </summary>
public class MatcherTests
{
    [Test]
    public async Task InRange_MatchesWithinBounds()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.IsInRange(1, 10), Arg.Any<int>()).Returns(99);

        // Act
        ICalculator calc = mock.Object;

        // Assert — min, max, and middle should all match
        await Assert.That(calc.Add(1, 0)).IsEqualTo(99);
        await Assert.That(calc.Add(10, 0)).IsEqualTo(99);
        await Assert.That(calc.Add(5, 0)).IsEqualTo(99);
    }

    [Test]
    public async Task InRange_RejectsOutsideBounds()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.IsInRange(1, 10), Arg.Any<int>()).Returns(99);

        // Act
        ICalculator calc = mock.Object;

        // Assert — below min and above max don't match, return default (0)
        await Assert.That(calc.Add(0, 0)).IsEqualTo(0);
        await Assert.That(calc.Add(-1, 0)).IsEqualTo(0);
        await Assert.That(calc.Add(11, 0)).IsEqualTo(0);
        await Assert.That(calc.Add(100, 0)).IsEqualTo(0);
    }

    [Test]
    public async Task IsIn_MatchesSetMembers()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.IsIn(1, 3, 5), Arg.Any<int>()).Returns(77);

        // Act
        ICalculator calc = mock.Object;

        // Assert — values in the set match
        await Assert.That(calc.Add(1, 0)).IsEqualTo(77);
        await Assert.That(calc.Add(3, 0)).IsEqualTo(77);
        await Assert.That(calc.Add(5, 0)).IsEqualTo(77);
    }

    [Test]
    public async Task IsIn_RejectsNonMembers()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.IsIn(1, 3, 5), Arg.Any<int>()).Returns(77);

        // Act
        ICalculator calc = mock.Object;

        // Assert — values not in the set don't match
        await Assert.That(calc.Add(0, 0)).IsEqualTo(0);
        await Assert.That(calc.Add(2, 0)).IsEqualTo(0);
        await Assert.That(calc.Add(4, 0)).IsEqualTo(0);
        await Assert.That(calc.Add(6, 0)).IsEqualTo(0);
    }

    [Test]
    public async Task IsNotIn_MatchesNonMembers()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.IsNotIn(1, 3, 5), Arg.Any<int>()).Returns(88);

        // Act
        ICalculator calc = mock.Object;

        // Assert — values NOT in the set match
        await Assert.That(calc.Add(0, 0)).IsEqualTo(88);
        await Assert.That(calc.Add(2, 0)).IsEqualTo(88);
        await Assert.That(calc.Add(4, 0)).IsEqualTo(88);
        await Assert.That(calc.Add(100, 0)).IsEqualTo(88);
    }

    [Test]
    public async Task IsNotIn_RejectsSetMembers()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.IsNotIn(1, 3, 5), Arg.Any<int>()).Returns(88);

        // Act
        ICalculator calc = mock.Object;

        // Assert — values in the set don't match, return default
        await Assert.That(calc.Add(1, 0)).IsEqualTo(0);
        await Assert.That(calc.Add(3, 0)).IsEqualTo(0);
        await Assert.That(calc.Add(5, 0)).IsEqualTo(0);
    }

    [Test]
    public async Task Not_NegatesInnerMatcher()
    {
        // Arrange — Not(Is(5)) should match everything except 5
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Not(Arg.Is(5)), Arg.Any<int>()).Returns(66);

        // Act
        ICalculator calc = mock.Object;

        // Assert — 3 matches (not 5), 5 does not match
        await Assert.That(calc.Add(3, 0)).IsEqualTo(66);
        await Assert.That(calc.Add(0, 0)).IsEqualTo(66);
        await Assert.That(calc.Add(-1, 0)).IsEqualTo(66);
        await Assert.That(calc.Add(5, 0)).IsEqualTo(0);
    }

    [Test]
    public async Task Not_WithPredicateMatcher()
    {
        // Arrange — Not(Is<int>(x => x > 0)) should match non-positive values
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Not(Arg.Is<int>(x => x > 0)), Arg.Any<int>()).Returns(55);

        // Act
        ICalculator calc = mock.Object;

        // Assert — negative and zero match, positive does not
        await Assert.That(calc.Add(-1, 0)).IsEqualTo(55);
        await Assert.That(calc.Add(0, 0)).IsEqualTo(55);
        await Assert.That(calc.Add(5, 0)).IsEqualTo(0);
        await Assert.That(calc.Add(100, 0)).IsEqualTo(0);
    }
}
