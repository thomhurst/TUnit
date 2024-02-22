using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

public class InstanceData
{
    private int _value;

    [Test]
    public void Test()
    {
        _value = 99;
    }

    [Test]
    public async Task Test2()
    {
        await Assert.That(_value).Is.EqualTo(99);
    }
}