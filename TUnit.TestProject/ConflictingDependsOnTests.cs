using System.Diagnostics.CodeAnalysis;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Failure)]
[UnconditionalSuppressMessage("Usage", "TUnit0033:Conflicting DependsOn attributes")]
public class ConflictingDependsOnTests
{
    [Test, DependsOn(nameof(Test2))]
    public async Task Test1()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    [Test, DependsOn(nameof(Test1))]
    public async Task Test2()
    {
        await Task.CompletedTask;
    }
}