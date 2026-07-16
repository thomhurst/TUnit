namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests to ensure that all combinations of nullable/non-nullable strings
/// work without generating nullability warnings (CS8604, CS8625, etc.)
/// This validates the fix for GitHub issue #3643.
/// </summary>
public class NullabilityWarningTests
{
    [Test]
    public async Task NonNullableString_WithNonNullableExpected_NoWarning()
    {
        string actualValue = "test";
        string expectedValue = "test";

        // Non-nullable to non-nullable should work
        await Assert.That(actualValue).IsEqualTo(expectedValue);
    }

    [Test]
    public async Task NonNullableString_WithNullableExpected_NoWarning()
    {
        string actualValue = "test";
        string? expectedValue = "test";

        // Non-nullable string should accept nullable expected without warning
        await Assert.That(actualValue).IsEqualTo(expectedValue);
    }

    [Test]
    public async Task NullableString_WithNonNullableExpected_NoWarning()
    {
        string? actualValue = "test";
        string expectedValue = "test";

        // Nullable string should accept non-nullable expected without warning
        await Assert.That(actualValue).IsEqualTo(expectedValue);
    }

    [Test]
    public async Task NullableString_WithNullableExpected_NoWarning()
    {
        string? actualValue = "test";
        string? expectedValue = "test";

        // Nullable to nullable should work (this is the main issue from #3643)
        await Assert.That(actualValue).IsEqualTo(expectedValue);
    }

    [Test]
    public async Task NonNullableString_WithNullLiteral_NoWarning()
    {
        string actualValue = "test";

        // Passing null literal should be allowed (tests for null mismatch)
        await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(actualValue).IsEqualTo(null));
    }

    [Test]
    public async Task NullableString_WithNullLiteral_NoWarning()
    {
        string? actualValue = null;

        // Nullable string with null literal should work
        await Assert.That(actualValue).IsEqualTo(null);
    }

    [Test]
    public async Task NonNullableString_WithStringComparison_NoWarning()
    {
        string actualValue = "TEST";
        string? expectedValue = "test";

        // Non-nullable with StringComparison parameter and nullable expected
        await Assert.That(actualValue).IsEqualTo(expectedValue, StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task NullableString_WithStringComparison_NoWarning()
    {
        string? actualValue = "TEST";
        string? expectedValue = "test";

        // Nullable with StringComparison parameter
        await Assert.That(actualValue).IsEqualTo(expectedValue, StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task NonNullableString_WithModifiers_NoWarning()
    {
        string actualValue = " TEST ";
        string? expectedValue = "test";

        // Non-nullable with modifiers and nullable expected
        await Assert.That(actualValue).IsEqualTo(expectedValue).WithTrimming().IgnoringCase();
    }

    [Test]
    public async Task NullableString_WithModifiers_NoWarning()
    {
        string? actualValue = " TEST ";
        string? expectedValue = "test";

        // Nullable with modifiers
        await Assert.That(actualValue).IsEqualTo(expectedValue).WithTrimming().IgnoringCase();
    }

    [Test]
    public async Task MixedNullability_FromMethods_NoWarning()
    {
        // Testing that method return values work correctly
        string nonNullable = GetNonNullableString();
        string? nullable = GetNullableString();

        await Assert.That(nonNullable).IsEqualTo(nullable);
        await Assert.That(nullable).IsEqualTo(nonNullable);
    }

    [Test]
    public async Task ImplicitConversion_NonNullableToNullable_NoWarning()
    {
        // This verifies that the generated extension method signature
        // IAssertionSource<string?> accepts both string and string? without warnings
        string nonNullable = "test";

        // The implicit conversion from string to string? should work seamlessly
        await Assert.That(nonNullable).IsEqualTo("test");
        await Assert.That(nonNullable).IsEqualTo((string?)"test");
    }

    private static string GetNonNullableString() => "result";

    private static string? GetNullableString() => "result";
}
