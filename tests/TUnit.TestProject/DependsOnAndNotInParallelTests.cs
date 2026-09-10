using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class DependsOnAndNotInParallelTests
{
    [Test, NotInParallel]
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
