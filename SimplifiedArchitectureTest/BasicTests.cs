using TUnit.Core;

namespace SimplifiedArchitectureTest;

public class BasicTests
{
    [Test]
    public void SimpleTest()
    {
        // Simple test passes
    }

    [Test]
    [Arguments(1, 2, 3)]
    [Arguments(10, 20, 30)]
    public void ParameterizedTest(int a, int b, int expected)
    {
        var result = a + b;
        if (result != expected)
        {
            throw new Exception($"Expected {expected} but got {result}");
        }
    }

    [Test]
    public async Task AsyncTest()
    {
        await Task.Delay(100);
    }
}