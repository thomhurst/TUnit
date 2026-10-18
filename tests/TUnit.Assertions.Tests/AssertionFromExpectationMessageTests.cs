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
[AssertionFrom<string>(nameof(string.Contains), CustomName = "HasTextSharingMethod", ExpectationMessage = "contain '{value}'")]
public static partial class DocumentedStyleStringAssertions;

// Same target method as an attribute above, but declared on another class and with another message.
[AssertionFrom<string>(nameof(string.IsNullOrEmpty), CustomName = "IsVacant", ExpectationMessage = "be vacant")]
public static partial class SharedMethodStringAssertions;

[AssertionFrom<string>(typeof(ExpectationMessageMethods), nameof(ExpectationMessageMethods.HasText), ExpectationMessage = "contain {{text}} ({text})")]
[AssertionFrom<string>(typeof(ExpectationMessageMethods), nameof(ExpectationMessageMethods.HasSuffix), ExpectationMessage = "end with {suffix}")]
[AssertionFrom<string>(typeof(ExpectationMessageMethods), nameof(ExpectationMessageMethods.IsEmptyText), ExpectationMessage = "be \"blank\" \\ empty")]
[AssertionFrom<string>(typeof(ExpectationMessageMethods), nameof(ExpectationMessageMethods.HasWanted), ExpectationMessage = "contain {önskad}")]
[AssertionFrom<string>(typeof(ExpectationMessageMethods), nameof(ExpectationMessageMethods.HasPrefix), ExpectationMessage = "start with {_prefix}")]
public static partial class EscapingStringAssertions;

// Three attributes on one method, each with its own message: the condition classes get numbered names.
[AssertionFrom<string>(nameof(string.EndsWith), CustomName = "EndsWithFirst", ExpectationMessage = "end with {value} (first)")]
[AssertionFrom<string>(nameof(string.EndsWith), CustomName = "EndsWithSecond", ExpectationMessage = "end with {value} (second)")]
[AssertionFrom<string>(nameof(string.EndsWith), CustomName = "EndsWithThird", ExpectationMessage = "end with {value} (third)")]
public static partial class ThreeMessagesStringAssertions;

/// <summary>
/// Bool-returning methods for the [AssertionFrom] declarations above, so each has its own target method and parameter
/// names that the messages refer to.
/// </summary>
public static class ExpectationMessageMethods
{
    public static bool HasText(string value, string text) => value.Contains(text);

    public static bool HasSuffix(string value, string ending) => value.EndsWith(ending, StringComparison.Ordinal);

    public static bool IsEmptyText(string value) => value.Length == 0;

    public static bool HasWanted(string value, string önskad) => value.Contains(önskad);

    public static bool HasPrefix(string value, string prefix) => value.StartsWith(prefix, StringComparison.Ordinal);
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

    [Test]
    public async Task Attributes_Sharing_A_Method_Keep_Their_Own_Messages()
    {
        await Assert.That(async () => await Assert.That("hello").HasTextSharingMethod("xyz"))
            .Throws<AssertionException>()
            .WithMessageContaining("Expected to contain 'xyz'");

        await Assert.That(async () => await Assert.That("hello").LacksTextDocumentedStyle("ell"))
            .Throws<AssertionException>()
            .WithMessageContaining("Expected to not contain 'ell'");
    }

    [Test]
    public async Task Attributes_Sharing_A_Method_Across_Classes_Keep_Their_Own_Messages()
    {
        await Assert.That(async () => await Assert.That("Foo").IsVacant())
            .Throws<AssertionException>()
            .WithMessageContaining("Expected to be vacant");

        await Assert.That(async () => await Assert.That("Foo").IsBlankDocumentedStyle())
            .Throws<AssertionException>()
            .WithMessageContaining("Expected to be null or empty");
    }

    [Test]
    public async Task Non_Ascii_Placeholder_Is_Substituted()
    {
        await Assert.That(async () => await Assert.That("hello").HasWanted("xyz"))
            .Throws<AssertionException>()
            .WithMessageContaining("Expected to contain xyz");
    }

    [Test]
    public async Task Generated_Field_Placeholder_Is_Still_Substituted()
    {
        await Assert.That(async () => await Assert.That("hello").HasPrefix("xyz"))
            .Throws<AssertionException>()
            .WithMessageContaining("Expected to start with xyz");
    }

    [Test]
    public async Task Three_Attributes_Sharing_A_Method_Keep_Their_Own_Messages()
    {
        await Assert.That(async () => await Assert.That("hello").EndsWithFirst("xyz"))
            .Throws<AssertionException>()
            .WithMessageContaining("Expected to end with xyz (first)");

        await Assert.That(async () => await Assert.That("hello").EndsWithSecond("xyz"))
            .Throws<AssertionException>()
            .WithMessageContaining("Expected to end with xyz (second)");

        await Assert.That(async () => await Assert.That("hello").EndsWithThird("xyz"))
            .Throws<AssertionException>()
            .WithMessageContaining("Expected to end with xyz (third)");
    }

    [Test]
    public async Task Classes_Sharing_A_Method_Get_Numbered_Names()
    {
        var assembly = typeof(AssertionFromExpectationMessageTests).Assembly;

        await Assert.That(assembly.GetType("TUnit.Assertions.Extensions.StringEndsWithWithStringAssertion")).IsNotNull();
        await Assert.That(assembly.GetType("TUnit.Assertions.Extensions.StringEndsWithWithStringAssertion2")).IsNotNull();
        await Assert.That(assembly.GetType("TUnit.Assertions.Extensions.StringEndsWithWithStringAssertion3")).IsNotNull();
    }
}
