using TUnit.Core;

namespace TUnit.TestProject;

public class FloatPrecisionTest
{
    [Test]
    [Arguments("helloworld", "HELLowORLD", 0.19999999f)]
    public async Task TestWithPreciseFloat(string arg1, string arg2, float floatValue)
    {
        await Assert.That(arg1).IsEqualTo("helloworld");
        await Assert.That(arg2).IsEqualTo("HELLowORLD");
        await Assert.That(floatValue).IsEqualTo(0.19999999f);
    }
    
    [Test]
    [Arguments(0.19999999f, 0.29999999f, 0.39999999f)]
    public async Task TestMultipleFloats(float f1, float f2, float f3)
    {
        await Assert.That(f1).IsEqualTo(0.19999999f);
        await Assert.That(f2).IsEqualTo(0.29999999f);
        await Assert.That(f3).IsEqualTo(0.39999999f);
    }
    
    [Test]
    [Arguments(0.1234567890123456789d)]
    public async Task TestDoublePrecision(double doubleValue)
    {
        await Assert.That(doubleValue).IsEqualTo(0.1234567890123456789d);
    }
}