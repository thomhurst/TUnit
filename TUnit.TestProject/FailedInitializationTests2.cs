using System.Diagnostics.CodeAnalysis;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Failure)]
[UnconditionalSuppressMessage("Usage", "TUnit0033:Conflicting DependsOn attributes")]
public class FailedInitializationTests2
{
    [Test]
    [DependsOn(nameof(Test2))]
    public void Test()
    {
    }

    [Test]
    [DependsOn(nameof(Test))]
    public void Test2()
    {
    }
}