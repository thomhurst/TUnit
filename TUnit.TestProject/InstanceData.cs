using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class InstanceData
{
    private int _value;

    [Test]
    public void Test()
    {
#pragma warning disable TUnit0018
        _value = 99;
#pragma warning restore TUnit0018
    }

    [Test]
    public async Task Test2()
    {
        await Assert.That(_value).IsEqualTo(99);
    }
}