using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core.Executors;

namespace TUnit.TestProject;

public class CultureTests
{
    [Test, Culture("en-GB")]
    public async Task Test1()
    {
        await Assert.That(double.Parse("3.5")).IsEqualTo(3.5);
    }
    
    [Test, InvariantCulture]
    public async Task Test2()
    {
        await Assert.That(double.Parse("3.5")).IsEqualTo(3.5);
    }
}