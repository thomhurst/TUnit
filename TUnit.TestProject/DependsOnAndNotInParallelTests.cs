using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class DependsOnAndNotInParallelTests
{
    [Test, NotInParallel]
    public async Task Test1(CancellationToken cancellationToken)
    {
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromSeconds(5), cancellationToken);
    }

    [Test, DependsOn(nameof(Test1))]
    public async Task Test2()
    {
        await Task.CompletedTask;
    }
}
