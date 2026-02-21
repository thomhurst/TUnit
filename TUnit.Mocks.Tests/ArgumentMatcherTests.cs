using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

/// <summary>
/// US3 Integration Tests: Argument matchers for mock setup expressions.
/// </summary>
public class ArgumentMatcherTests
{
    [Test]
    public async Task Arg_Any_Matches_All_Values()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(42);

        // Act
        ICalculator calc = mock.Object;

        // Assert — all calls should match and return 42
        await Assert.That(calc.Add(0, 0)).IsEqualTo(42);
        await Assert.That(calc.Add(1, 2)).IsEqualTo(42);
        await Assert.That(calc.Add(-100, 999)).IsEqualTo(42);
        await Assert.That(calc.Add(int.MaxValue, int.MinValue)).IsEqualTo(42);
    }

    [Test]
    public async Task Arg_Is_With_Predicate_Matches_When_True()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Is<int>(a => a > 0), Arg.Is<int>(b => b > 0)).Returns(100);

        // Act
        ICalculator calc = mock.Object;

        // Assert — positive values match
        await Assert.That(calc.Add(1, 2)).IsEqualTo(100);
        await Assert.That(calc.Add(50, 50)).IsEqualTo(100);

        // Assert — non-positive values do not match, return default (0)
        await Assert.That(calc.Add(0, 1)).IsEqualTo(0);
        await Assert.That(calc.Add(-1, 5)).IsEqualTo(0);
        await Assert.That(calc.Add(1, -1)).IsEqualTo(0);
    }

    [Test]
    public async Task Arg_Is_With_Exact_Value()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Is(10), Arg.Is(20)).Returns(30);

        // Act
        ICalculator calc = mock.Object;

        // Assert — exact match
        await Assert.That(calc.Add(10, 20)).IsEqualTo(30);

        // Assert — non-match returns default
        await Assert.That(calc.Add(10, 21)).IsEqualTo(0);
        await Assert.That(calc.Add(11, 20)).IsEqualTo(0);
    }

    [Test]
    public async Task Arg_Capture_Captures_Values()
    {
        // Arrange
        var firstArg = Arg.Any<int>();
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(firstArg, Arg.Any<int>()).Returns(1);

        // Act
        ICalculator calc = mock.Object;
        calc.Add(10, 0);
        calc.Add(20, 0);
        calc.Add(30, 0);

        // Assert — captured values
        await Assert.That(firstArg.Values).Count().IsEqualTo(3);
        await Assert.That(firstArg.Values[0]).IsEqualTo(10);
        await Assert.That(firstArg.Values[1]).IsEqualTo(20);
        await Assert.That(firstArg.Values[2]).IsEqualTo(30);
        await Assert.That(firstArg.Latest).IsEqualTo(30);
    }

    [Test]
    public async Task Mixed_Matchers_And_Exact_Values()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        // First arg: any int. Second arg: exact 5.
        mock.Setup.Add(Arg.Any<int>(), 5).Returns(99);

        // Act
        ICalculator calc = mock.Object;

        // Assert — second arg must be 5, first can be anything
        await Assert.That(calc.Add(0, 5)).IsEqualTo(99);
        await Assert.That(calc.Add(100, 5)).IsEqualTo(99);
        await Assert.That(calc.Add(-1, 5)).IsEqualTo(99);

        // Assert — second arg is not 5, so no match
        await Assert.That(calc.Add(0, 4)).IsEqualTo(0);
        await Assert.That(calc.Add(0, 6)).IsEqualTo(0);
    }

    [Test]
    public async Task Arg_IsNull_Matches_Null_Values()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>();
        mock.Setup.Greet(Arg.IsNull<string>()).Returns("got null");

        // Act
        IGreeter greeter = mock.Object;

        // Assert — null matches
        await Assert.That(greeter.Greet(null!)).IsEqualTo("got null");

        // Assert — non-null does not match, returns default
        await Assert.That(greeter.Greet("Alice")).IsNotEqualTo("got null");
    }

    [Test]
    public async Task Arg_IsNotNull_Matches_NonNull_Values()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>();
        mock.Setup.Greet(Arg.IsNotNull<string>()).Returns("got something");

        // Act
        IGreeter greeter = mock.Object;

        // Assert — non-null matches
        await Assert.That(greeter.Greet("Alice")).IsEqualTo("got something");
        await Assert.That(greeter.Greet("Bob")).IsEqualTo("got something");
        await Assert.That(greeter.Greet("")).IsEqualTo("got something");

        // Assert — null does not match
        await Assert.That(greeter.Greet(null!)).IsNotEqualTo("got something");
    }

    [Test]
    public async Task Arg_Capture_With_String_Values()
    {
        // Arrange
        var nameArg = Arg.Any<string>();
        var mock = Mock.Of<IGreeter>();
        mock.Setup.Greet(nameArg).Returns("hi");

        // Act
        IGreeter greeter = mock.Object;
        greeter.Greet("Alice");
        greeter.Greet("Bob");

        // Assert — captured string values
        await Assert.That(nameArg.Values).Count().IsEqualTo(2);
        await Assert.That(nameArg.Values[0]).IsEqualTo("Alice");
        await Assert.That(nameArg.Values[1]).IsEqualTo("Bob");
        await Assert.That(nameArg.Latest).IsEqualTo("Bob");
    }

    [Test]
    public async Task Multiple_Setups_With_Different_Matchers()
    {
        // Arrange — more specific setup first, then broader
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(1, 1).Returns(100);
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(42);

        // Act
        ICalculator calc = mock.Object;

        // Assert — Any matcher is last, so it wins for all calls (last wins)
        await Assert.That(calc.Add(1, 1)).IsEqualTo(42);
        await Assert.That(calc.Add(2, 3)).IsEqualTo(42);
    }

    [Test]
    public async Task Specific_Setup_After_Any_Takes_Precedence()
    {
        // Arrange — broad setup first, then specific
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(42);
        mock.Setup.Add(1, 1).Returns(100);

        // Act
        ICalculator calc = mock.Object;

        // Assert — (1,1) matches the specific setup (last wins), others match Any
        await Assert.That(calc.Add(1, 1)).IsEqualTo(100);
        await Assert.That(calc.Add(2, 3)).IsEqualTo(42);
    }

    [Test]
    public async Task Arg_Capture_Latest_Returns_Default_When_Empty()
    {
        // Arrange
        var arg = Arg.Any<int>();

        // Assert — no calls yet
        await Assert.That(arg.Values).Count().IsEqualTo(0);
        await Assert.That(arg.Latest).IsEqualTo(0);
    }

    [Test]
    public async Task Arg_Capture_Does_Not_Capture_On_Partial_Match()
    {
        // Arrange — setup requires first arg = any (captured), second arg starts with "prefix"
        var firstArg = Arg.Any<int>();
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(firstArg, Arg.Is<int>(b => b > 100)).Returns(999);

        ICalculator calc = mock.Object;

        // Act — call with b=5 which does NOT satisfy the predicate matcher
        calc.Add(42, 5);

        // Assert — 42 should NOT be captured because the overall setup did not match
        await Assert.That(firstArg.Values).Count().IsEqualTo(0);

        // Act — now call with b=200 which DOES satisfy the predicate
        var result = calc.Add(7, 200);

        // Assert — only 7 is captured (from the matching call), not 42
        await Assert.That(firstArg.Values).Count().IsEqualTo(1);
        await Assert.That(firstArg.Values[0]).IsEqualTo(7);
        await Assert.That(result).IsEqualTo(999);
    }

    [Test]
    public async Task Predicate_Matcher_With_String()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>();
        mock.Setup.Greet(Arg.Is<string>(s => s != null && s.StartsWith("A"))).Returns("starts with A");

        // Act
        IGreeter greeter = mock.Object;

        // Assert
        await Assert.That(greeter.Greet("Alice")).IsEqualTo("starts with A");
        await Assert.That(greeter.Greet("Anna")).IsEqualTo("starts with A");
        await Assert.That(greeter.Greet("Bob")).IsNotEqualTo("starts with A");
    }
}
