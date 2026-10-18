using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests;

/// <summary>
/// [AssertionFrom] expectation messages written in the documented style, which spells out the leading "to"
/// (and, with NegateLogic, the "not") that the generator otherwise adds itself.
/// </summary>
[AssertionFrom<string>(nameof(string.IsNullOrEmpty), CustomName = "IsBlankDocumentedStyle", ExpectationMessage = "to be null or empty")]
[AssertionFrom<string>(nameof(string.IsNullOrEmpty), CustomName = "IsNotBlankDocumentedStyle", NegateLogic = true, ExpectationMessage = "to be null or empty")]
[AssertionFrom<string>(nameof(string.IsNullOrWhiteSpace), CustomName = "HasTextDocumentedStyle", NegateLogic = true, ExpectationMessage = "to not be null, empty, or whitespace")]
[AssertionFrom<string>(nameof(string.StartsWith), CustomName = "BeginsWithTextDocumentedStyle", ExpectationMessage = "to start with '{value}'")]
[AssertionFrom<string>(nameof(string.Contains), CustomName = "LacksTextDocumentedStyle", NegateLogic = true, ExpectationMessage = "to not contain '{value}'")]
public static partial class DocumentedStyleStringAssertions;

// The generator shares one condition class per target method, so these use their own methods to get their own messages.
[AssertionFrom<string>(typeof(ExpectationMessageMethods), nameof(ExpectationMessageMethods.HasText), ExpectationMessage = "contain {{text}} ({text})")]
[AssertionFrom<string>(typeof(ExpectationMessageMethods), nameof(ExpectationMessageMethods.HasSuffix), ExpectationMessage = "end with {suffix}")]
[AssertionFrom<string>(typeof(ExpectationMessageMethods), nameof(ExpectationMessageMethods.IsEmptyText), ExpectationMessage = "be \"blank\" \\ empty")]
public static partial class EscapingStringAssertions;

public static class ExpectationMessageMethods
{
    public static bool HasText(string value, string text) => value.Contains(text);

    public static bool HasSuffix(string value, string ending) => value.EndsWith(ending, StringComparison.Ordinal);

    public static bool IsEmptyText(string value) => value.Length == 0;
}

public class AssertionFromExpectationMessageTests
{
    [Test]
    public async Task Leading_To_Is_Not_Repeated()
    {
        await Assert.That(async () => await Assert.That("Foo").IsBlankDocumentedStyle())
            .Throws<AssertionException>()
            .WithMessageContaining("Expected to be null or empty");
    }

    [Test]
    public async Task Leading_To_Is_Not_Repeated_When_Negated()
    {
        await Assert.That(async () => await Assert.That("").IsNotBlankDocumentedStyle())
            .Throws<AssertionException>()
            .WithMessageContaining("Expected to not be null or empty");
    }

    [Test]
    public async Task Spelled_Out_Negation_Is_Not_Repeated()
    {
        await Assert.That(async () => await Assert.That(" ").HasTextDocumentedStyle())
            .Throws<AssertionException>()
            .WithMessageContaining("Expected to not be null, empty, or whitespace");
    }

    [Test]
    public async Task Parameter_Placeholder_Is_Substituted()
    {
        await Assert.That(async () => await Assert.That("hello").BeginsWithTextDocumentedStyle("xyz"))
            .Throws<AssertionException>()
            .WithMessageContaining("Expected to start with 'xyz'");
    }

    [Test]
    public async Task Parameter_Placeholder_Is_Substituted_With_Spelled_Out_Negation()
    {
        await Assert.That(async () => await Assert.That("hello").LacksTextDocumentedStyle("ell"))
            .Throws<AssertionException>()
            .WithMessageContaining("Expected to not contain 'ell'");
    }

    [Test]
    public async Task Escaped_Braces_Stay_Literal()
    {
        await Assert.That(async () => await Assert.That("hello").HasText("xyz"))
            .Throws<AssertionException>()
            .WithMessageContaining("Expected to contain {text} (xyz)");
    }

    [Test]
    public async Task Placeholder_That_Names_No_Parameter_Stays_Literal()
    {
        await Assert.That(async () => await Assert.That("hello").HasSuffix("xyz"))
            .Throws<AssertionException>()
            .WithMessageContaining("Expected to end with {suffix}");
    }

    [Test]
    public async Task Quotes_And_Backslashes_Stay_Literal()
    {
        await Assert.That(async () => await Assert.That("Foo").IsEmptyText())
            .Throws<AssertionException>()
            .WithMessageContaining("Expected to be \"blank\" \\ empty");
    }
}
