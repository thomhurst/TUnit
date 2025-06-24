using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TestNewArchitecture;

public class SimpleTests
{
    [Test]
    public async Task Test1()
    {
        await Assert.That(1 + 1).IsEqualTo(2);
    }
    
    [Test]
    public async Task Test2()
    {
        await Assert.That("hello").IsNotNull();
    }
    
    [Test]
    [Arguments(1, 2, 3)]
    [Arguments(2, 3, 5)]
    public async Task TestWithArgs(int a, int b, int expected)
    {
        await Assert.That(a + b).IsEqualTo(expected);
    }
}