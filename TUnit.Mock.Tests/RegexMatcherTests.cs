using System.Text.RegularExpressions;
using TUnit.Mock.Arguments;

namespace TUnit.Mock.Tests;

/// <summary>
/// US12 Tests: Regex argument matching for string parameters.
/// </summary>
public class RegexMatcherTests
{
    [Test]
    public async Task Arg_Matches_With_Pattern_Matches_Matching_Strings()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>();
        mock.Setup.Greet(Arg.Matches(@"^[A-Z]")).Returns("capitalized");

        // Act
        var greeter = mock.Object;

        // Assert
        await Assert.That(greeter.Greet("Alice")).IsEqualTo("capitalized");
        await Assert.That(greeter.Greet("Bob")).IsEqualTo("capitalized");
    }

    [Test]
    public async Task Arg_Matches_With_Pattern_Does_Not_Match_NonMatching()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>();
        mock.Setup.Greet(Arg.Matches(@"^[A-Z]")).Returns("capitalized");

        // Act
        var greeter = mock.Object;

        // Assert — lowercase doesn't match, returns default (empty string)
        await Assert.That(greeter.Greet("alice")).IsEmpty();
        await Assert.That(greeter.Greet("bob")).IsEmpty();
    }

    [Test]
    public async Task Arg_Matches_With_Regex_Object()
    {
        // Arrange
        var regex = new Regex(@"\d{3}-\d{4}", RegexOptions.Compiled);
        var mock = Mock.Of<IGreeter>();
        mock.Setup.Greet(Arg.Matches(regex)).Returns("phone");

        // Act
        var greeter = mock.Object;

        // Assert
        await Assert.That(greeter.Greet("555-1234")).IsEqualTo("phone");
        await Assert.That(greeter.Greet("hello")).IsEmpty();
    }

    [Test]
    public async Task Arg_Matches_Regex_Does_Not_Match_Null()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>();
        mock.Setup.Greet(Arg.Matches(".*")).Returns("matched");

        // Act
        var greeter = mock.Object;

        // Assert — null should not match regex, returns default (empty string)
        await Assert.That(greeter.Greet(null!)).IsEmpty();
    }

    [Test]
    public async Task Arg_Matches_Regex_With_Email_Pattern()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>();
        mock.Setup.Greet(Arg.Matches(@"^[\w.+-]+@[\w-]+\.[\w.]+$")).Returns("email");

        // Act
        var greeter = mock.Object;

        // Assert
        await Assert.That(greeter.Greet("user@example.com")).IsEqualTo("email");
        await Assert.That(greeter.Greet("not-an-email")).IsEmpty();
    }
}
