namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Regression tests for GitHub issue #5155:
/// StringEqualsAssertion was generic with no constraints, causing IsEqualTo to
/// incorrectly match non-string types when expected value was a string.
/// The actual value was silently cast to null via `as string`, producing "received """.
/// </summary>
public class Issue5155Tests
{
    public sealed record StringEquatableRecord(string Value) : IEquatable<string>
    {
        public bool Equals(string? other) => Value.Equals(other);
    }

    /// <summary>
    /// Verifies that IsEquatableTo works for types implementing IEquatable&lt;string&gt;.
    /// This was already working before the fix.
    /// </summary>
    [Test]
    public async Task IsEquatableTo_WithStringEquatableType_Succeeds()
    {
        var test = new StringEquatableRecord("test");
        await Assert.That(test).IsEquatableTo("test");
    }

    /// <summary>
    /// Verifies that IsEquatableTo correctly fails when values don't match.
    /// </summary>
    [Test]
    public async Task IsEquatableTo_WithStringEquatableType_FailsWhenNotEqual()
    {
        var test = new StringEquatableRecord("test");
        await Assert.ThrowsAsync<AssertionException>(async () =>
        {
            await Assert.That(test).IsEquatableTo("other");
        });
    }

    /// <summary>
    /// Regression: Verifies that string IsEqualTo still works correctly after
    /// making StringEqualsAssertion non-generic.
    /// </summary>
    [Test]
    public async Task String_IsEqualTo_StillWorks()
    {
        await Assert.That("hello").IsEqualTo("hello");
    }

    /// <summary>
    /// Regression: Verifies that string IsEqualTo with IgnoringCase still works.
    /// </summary>
    [Test]
    public async Task String_IsEqualTo_IgnoringCase_StillWorks()
    {
        await Assert.That("Hello").IsEqualTo("hello").IgnoringCase();
    }

    /// <summary>
    /// Regression: Verifies that string IsEqualTo with WithTrimming still works.
    /// </summary>
    [Test]
    public async Task String_IsEqualTo_WithTrimming_StillWorks()
    {
        await Assert.That("  hello  ").IsEqualTo("hello").WithTrimming();
    }

    /// <summary>
    /// Regression: Verifies that string IsEqualTo with IgnoringWhitespace still works.
    /// </summary>
    [Test]
    public async Task String_IsEqualTo_IgnoringWhitespace_StillWorks()
    {
        await Assert.That("h e l l o").IsEqualTo("hello").IgnoringWhitespace();
    }

    /// <summary>
    /// Regression: Verifies that string IsEqualTo with WithNullAndEmptyEquality still works.
    /// </summary>
    [Test]
    public async Task String_IsEqualTo_NullAndEmptyEquality_StillWorks()
    {
        string? value = null;
        await Assert.That(value).IsEqualTo("").WithNullAndEmptyEquality();
    }

    /// <summary>
    /// Regression: Verifies that string IsEqualTo failure produces correct error message.
    /// </summary>
    [Test]
    public async Task String_IsEqualTo_FailureMessage_IsCorrect()
    {
        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
        {
            await Assert.That("actual").IsEqualTo("expected");
        });

        await Assert.That(exception!.Message).Contains("received \"actual\"");
    }

    /// <summary>
    /// Regression: Verifies that string IsEqualTo with StringComparison still works.
    /// </summary>
    [Test]
    public async Task String_IsEqualTo_WithStringComparison_StillWorks()
    {
        await Assert.That("hello").IsEqualTo("HELLO", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that IsEqualTo with same-type record comparison still works.
    /// </summary>
    [Test]
    public async Task Record_IsEqualTo_SameType_StillWorks()
    {
        var test1 = new StringEquatableRecord("test");
        var test2 = new StringEquatableRecord("test");
        await Assert.That(test1).IsEqualTo(test2);
    }
}
