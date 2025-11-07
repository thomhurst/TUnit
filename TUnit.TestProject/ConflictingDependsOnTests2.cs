using System.Diagnostics.CodeAnalysis;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Failure)]
[UnconditionalSuppressMessage("Usage", "TUnit0033:Conflicting DependsOn attributes")]
public class ConflictingDependsOnTests2
{
    [Test, DependsOn(nameof(Test3))]
    public async Task Test1(CancellationToken cancellationToken)
    {
        var timeProvider = TestContext.Current!.GetService<TimeProvider>();
        await timeProvider.Delay(TimeSpan.FromSeconds(5), cancellationToken);
    }

    [Test, DependsOn(nameof(Test1))]
    public async Task Test2()
    {
        await Task.CompletedTask;
    }

    [Test, DependsOn(nameof(Test2))]
    public async Task Test3()
    {
        await Task.CompletedTask;
    }
}
