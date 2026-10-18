using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests;

/// <summary>
/// [AssertionFrom] expectation messages written in the documented style, which spells out the leading "to"
/// (and, with NegateLogic, the "not") that the generator otherwise adds itself.
/// </summary>
[AssertionFrom<string>(nameof(string.IsNullOrEmpty), CustomName = "IsBlankDocumentedStyle", ExpectationMessage = "to be null or empty")]
[AssertionFrom<string>(nameof(string.IsNullOrEmpty), CustomName = "IsNotBlankDocumentedStyle", NegateLogic = true, ExpectationMessage = "to be null or empty")]
[AssertionFrom<string>(nameof(string.IsNullOrWhiteSpace), CustomName = "HasTextDocumentedStyle", NegateLogic = true, ExpectationMessage = "to not be null, empty, or whitespace")]
public static partial class DocumentedStyleStringAssertions;

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
}
