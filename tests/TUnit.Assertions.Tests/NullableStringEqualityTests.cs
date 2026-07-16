namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests to verify that IsEqualTo works correctly with nullable strings
/// without requiring workarounds like `?? string.Empty`.
/// Addresses GitHub issue #3643.
/// </summary>
public class NullableStringEqualityTests
{
    [Test]
    public async Task IsEqualTo_WithNullableStringActualAndNullableStringExpected_BothNonNull_Succeeds()
    {
        string? actualValue = "test";
        string? expectedValue = "test";

        // This should work without requiring `expectedValue ?? string.Empty`
        await Assert.That(actualValue).IsEqualTo(expectedValue);
    }

    [Test]
    public async Task IsEqualTo_WithNullableStringActualAndNullableStringExpected_BothNull_Succeeds()
    {
        string? actualValue = null;
        string? expectedValue = null;

        // Both null should be considered equal
        await Assert.That(actualValue).IsEqualTo(expectedValue);
    }

    [Test]
    public async Task IsEqualTo_WithNullableStringActualAndNullExpected_ActualNonNull_Fails()
    {
        string? actualValue = "test";
        string? expectedValue = null;

        await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(actualValue).IsEqualTo(expectedValue));
    }

    [Test]
    public async Task IsEqualTo_WithNullableStringActualAndNullExpected_ActualNull_Succeeds()
    {
        string? actualValue = null;
        string? expectedValue = "test";

        await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(actualValue).IsEqualTo(expectedValue));
    }

    [Test]
    public async Task IsEqualTo_WithNullableString_AndIgnoringCase_Succeeds()
    {
        string? actualValue = "TEST";
        string? expectedValue = "test";

        // Nullable strings should work with string-specific modifiers
        await Assert.That(actualValue).IsEqualTo(expectedValue).IgnoringCase();
    }

    [Test]
    public async Task IsEqualTo_WithNullableString_AndWithTrimming_Succeeds()
    {
        string? actualValue = " test ";
        string? expectedValue = "test";

        // Nullable strings should work with string-specific modifiers
        await Assert.That(actualValue).IsEqualTo(expectedValue).WithTrimming();
    }

    [Test]
    public async Task IsEqualTo_WithNullableString_AndIgnoringWhitespace_Succeeds()
    {
        string? actualValue = "t e s t";
        string? expectedValue = "test";

        // Nullable strings should work with string-specific modifiers
        await Assert.That(actualValue).IsEqualTo(expectedValue).IgnoringWhitespace();
    }

    [Test]
    public async Task IsEqualTo_WithNullableString_AndWithNullAndEmptyEquality_NullActualEmptyExpected_Succeeds()
    {
        string? actualValue = null;
        string? expectedValue = "";

        // null and empty should be considered equal with this modifier
        await Assert.That(actualValue).IsEqualTo(expectedValue).WithNullAndEmptyEquality();
    }

    [Test]
    public async Task IsEqualTo_WithNullableString_AndWithNullAndEmptyEquality_EmptyActualNullExpected_Succeeds()
    {
        string? actualValue = "";
        string? expectedValue = null;

        // null and empty should be considered equal with this modifier
        await Assert.That(actualValue).IsEqualTo(expectedValue).WithNullAndEmptyEquality();
    }

    [Test]
    public async Task IsEqualTo_WithNullableString_AndWithNullAndEmptyEquality_BothNull_Succeeds()
    {
        string? actualValue = null;
        string? expectedValue = null;

        await Assert.That(actualValue).IsEqualTo(expectedValue).WithNullAndEmptyEquality();
    }

    [Test]
    public async Task IsEqualTo_WithNullableString_AndWithComparison_Succeeds()
    {
        string? actualValue = "TEST";
        string? expectedValue = "test";

        // Nullable strings should work with StringComparison parameter
        await Assert.That(actualValue).IsEqualTo(expectedValue).WithComparison(StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task IsEqualTo_WithNullableString_CombinedModifiers_Succeeds()
    {
        string? actualValue = "  T E S T  ";
        string? expectedValue = "test";

        // Multiple modifiers should work together with nullable strings
        await Assert.That(actualValue).IsEqualTo(expectedValue).WithTrimming().IgnoringWhitespace().IgnoringCase();
    }

    [Test]
    public async Task IsEqualTo_WithNullableVariableDeclaredWithVar_Succeeds()
    {
        // Verify it works with var (type inference) as well
        string? actual = GetNullableString();
        string? expected = "result";

        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task IsEqualTo_WithNullableStringFromMethod_BothNull_Succeeds()
    {
        string? actual = GetNullString();
        string? expected = GetNullString();

        await Assert.That(actual).IsEqualTo(expected);
    }

    private static string? GetNullableString() => "result";

    private static string? GetNullString() => null;
}
