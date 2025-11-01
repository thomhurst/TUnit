using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests to ensure nullable string assertions work correctly without compiler warnings.
/// Addresses issue: "Assertion On Nullable String Actual Value Requires Non-Nullable String Expected Value"
/// </summary>
public class NullableStringEqualityTests
{
    [Test]
    public async Task IsEqualTo_WithNullableStringActualAndNullableStringExpected_Works()
    {
        string? actualValue = "test";
        string? expectedValue = "test";
        
        // This should work without requiring `expectedValue ?? string.Empty`
        await Assert.That(actualValue).IsEqualTo(expectedValue);
    }
    
    [Test]
    public async Task IsEqualTo_WithNullableStringActualAndNullExpected_Works()
    {
        string? actualValue = null;
        string? expectedValue = null;
        
        await Assert.That(actualValue).IsEqualTo(expectedValue);
    }
    
    [Test]
    public async Task IsEqualTo_WithNullableStringActualAndLiteralNull_Works()
    {
        string? actualValue = null;
        
        await Assert.That(actualValue).IsEqualTo(null);
    }
    
    [Test]
    public async Task IsEqualTo_WithNonNullableStringActualAndNullableStringExpected_Works()
    {
        string actualValue = "test";
        string? expectedValue = "test";
        
        await Assert.That(actualValue).IsEqualTo(expectedValue);
    }
    
    [Test]
    public async Task IsEqualTo_WithNullableFromMethodAndNullableExpected_Works()
    {
        string? actualValue = GetNullableString();
        string? expectedValue = GetNullableString();
        
        await Assert.That(actualValue).IsEqualTo(expectedValue);
    }
    
    [Test]
    public async Task IsEqualTo_WithNullableStringActualAndNullableStringExpected_Fails_WhenDifferent()
    {
        string? actualValue = "test1";
        string? expectedValue = "test2";
        
        await Assert.That(async () => await Assert.That(actualValue).IsEqualTo(expectedValue))
            .Throws<AssertionException>();
    }
    
    [Test]
    public async Task IsEqualTo_WithStringComparison_AndNullableStrings_Works()
    {
        string? actualValue = "TEST";
        string? expectedValue = "test";
        
        await Assert.That(actualValue).IsEqualTo(expectedValue, StringComparison.OrdinalIgnoreCase);
    }
    
    private static string? GetNullableString() => "test";
}
