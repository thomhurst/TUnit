using TUnit.Core;
using System.Threading.Tasks;

namespace SimpleTest;

public class BasicTests
{
    [Test]
    public async Task SimpleTest()
    {
        await Task.Delay(10);
        // Test passes
    }
    
    [Test]
    [Arguments(1, 2, 3)]
    [Arguments(4, 5, 9)]
    public void TestWithArguments(int a, int b, int expectedSum)
    {
        var sum = a + b;
        if (sum != expectedSum)
            throw new System.Exception($"Expected {expectedSum} but got {sum}");
    }
}