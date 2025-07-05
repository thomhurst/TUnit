using TUnit.Core;
using TUnit.Assertions;
using System.Threading.Tasks;

namespace TUnit.TestProject;

public class SimpleArgumentsTest
{
    [Test]
    [Arguments(42)]
    public async Task JustOneArgument(int value)
    {
        await Assert.That(value).IsEqualTo(42);
    }
}