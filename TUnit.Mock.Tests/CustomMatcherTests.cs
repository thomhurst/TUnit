using TUnit.Mock.Arguments;
using TUnit.Mock.Verification;

namespace TUnit.Mock.Tests;

/// <summary>
/// Custom matcher that checks string length range.
/// </summary>
public class StringLengthMatcher : IArgumentMatcher<string>
{
    private readonly int _min;
    private readonly int _max;

    public StringLengthMatcher(int min, int max)
    {
        _min = min;
        _max = max;
    }

    public bool Matches(string? value) => value is not null && value.Length >= _min && value.Length <= _max;

    public bool Matches(object? value) => value is string s && Matches(s);

    public string Describe() => $"StringLengthMatcher({_min}..{_max})";
}

/// <summary>
/// Custom matcher that checks integer range.
/// </summary>
public class RangeMatcher : IArgumentMatcher<int>
{
    private readonly int _min;
    private readonly int _max;

    public RangeMatcher(int min, int max)
    {
        _min = min;
        _max = max;
    }

    public bool Matches(int value) => value >= _min && value <= _max;

    public bool Matches(object? value) => value is int i && Matches(i);

    public string Describe() => $"RangeMatcher({_min}..{_max})";
}

/// <summary>
/// US12 Tests: Custom argument matchers via IArgumentMatcher&lt;T&gt;.
/// </summary>
public class CustomMatcherTests
{
    [Test]
    public async Task Custom_String_Length_Matcher()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>();
        mock.Setup.Greet(Arg.Matches(new StringLengthMatcher(3, 10))).Returns("valid");

        // Act
        var greeter = mock.Object;

        // Assert
        await Assert.That(greeter.Greet("Alice")).IsEqualTo("valid");
        await Assert.That(greeter.Greet("Bob")).IsEqualTo("valid");
        await Assert.That(greeter.Greet("Hi")).IsEmpty(); // too short
        await Assert.That(greeter.Greet("A very long name indeed")).IsEmpty(); // too long
    }

    [Test]
    public async Task Custom_Range_Matcher_With_Calculator()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Matches(new RangeMatcher(1, 10)), Arg.Any<int>()).Returns(100);

        // Act
        var calc = mock.Object;

        // Assert
        await Assert.That(calc.Add(5, 0)).IsEqualTo(100);
        await Assert.That(calc.Add(1, 999)).IsEqualTo(100);
        await Assert.That(calc.Add(10, 0)).IsEqualTo(100);
        await Assert.That(calc.Add(0, 5)).IsEqualTo(0); // first arg out of range
        await Assert.That(calc.Add(11, 0)).IsEqualTo(0); // first arg out of range
    }

    [Test]
    public async Task Custom_Matcher_With_Predicate_Combined()
    {
        // Arrange — custom matcher for first arg, predicate for second
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Matches(new RangeMatcher(0, 100)), Arg.Is<int>(b => b > 0)).Returns(42);

        // Act
        var calc = mock.Object;

        // Assert
        await Assert.That(calc.Add(50, 10)).IsEqualTo(42);
        await Assert.That(calc.Add(0, 1)).IsEqualTo(42);
        await Assert.That(calc.Add(50, 0)).IsEqualTo(0);  // second arg not > 0
        await Assert.That(calc.Add(101, 10)).IsEqualTo(0); // first arg out of range
    }

    [Test]
    public async Task Custom_Matcher_Describe_Used_In_Error()
    {
        // Arrange
        var matcher = new StringLengthMatcher(5, 20);

        // Assert — Describe returns a readable description
        await Assert.That(matcher.Describe()).IsEqualTo("StringLengthMatcher(5..20)");
    }

    [Test]
    public async Task Custom_Matcher_With_Verification()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(0);

        // Act
        var calc = mock.Object;
        calc.Add(5, 10);
        calc.Add(50, 100);

        // Verify — using custom matcher in verification
        mock.Verify.Add(Arg.Matches(new RangeMatcher(1, 10)), Arg.Any<int>()).WasCalled(Times.Once);
        mock.Verify.Add(Arg.Matches(new RangeMatcher(40, 60)), Arg.Any<int>()).WasCalled(Times.Once);
    }
}
