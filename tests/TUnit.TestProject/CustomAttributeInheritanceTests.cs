using TUnit.Core;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[SkipNetFramework("Not supported on .NET Framework")]
public class CustomAttributeInheritanceTests
{
    [Test]
    [EngineTest(ExpectedResult.Pass)]
    public void Test()
    {
    }
}
