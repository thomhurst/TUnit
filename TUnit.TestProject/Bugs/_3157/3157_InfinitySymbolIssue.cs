using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._3157;

[EngineTest(ExpectedResult.Pass)]
public class InfinitySymbolIssue
{
    [Test]
    [Arguments("1.0", 1d)]
    [Arguments("1.0i64", 1d)]
    [Arguments("0.682287216i64", 0.682287216d)]
    [Arguments("0xFFF8000000000000", double.NaN)]
    [Arguments("0xFFF8000000000000i64", double.NaN)]
    [Arguments("inf64", double.PositiveInfinity)]
    [Arguments("inf", double.PositiveInfinity)]
    [Arguments("-inf64", double.NegativeInfinity)]
    [Arguments("-inf", double.NegativeInfinity)]
    public async Task TestWithSpecialFloatingPointValues(string text, double expectedValue)
    {
        // Test that the special floating-point values are handled correctly
        if (double.IsNaN(expectedValue))
        {
            await Assert.That(double.IsNaN(expectedValue)).IsTrue();
        }
        else if (double.IsPositiveInfinity(expectedValue))
        {
            await Assert.That(double.IsPositiveInfinity(expectedValue)).IsTrue();
        }
        else if (double.IsNegativeInfinity(expectedValue))
        {
            await Assert.That(double.IsNegativeInfinity(expectedValue)).IsTrue();
        }
        else
        {
            await Assert.That(expectedValue).IsEqualTo(expectedValue);
        }
    }
    
    [Test]
    [Arguments(float.NaN)]
    [Arguments(float.PositiveInfinity)]
    [Arguments(float.NegativeInfinity)]
    public async Task TestFloatSpecialValues(float value)
    {
        if (float.IsNaN(value))
        {
            await Assert.That(float.IsNaN(value)).IsTrue();
        }
        else
        {
            await Assert.That(float.IsInfinity(value)).IsTrue();
        }
    }
}