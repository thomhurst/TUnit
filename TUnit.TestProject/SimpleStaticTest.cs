using TUnit.Core;

namespace TUnit.TestProject;

public class SimpleStaticTest
{
    [Test]
    public async Task SimpleTest()
    {
        Console.WriteLine("Simple test running");
        await Assert.That(1).IsEqualTo(1);
    }
    
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    public async Task TestWithArguments(int value)
    {
        Console.WriteLine($"Testing with value: {value}");
        await Assert.That(value).IsGreaterThan(0);
    }
}