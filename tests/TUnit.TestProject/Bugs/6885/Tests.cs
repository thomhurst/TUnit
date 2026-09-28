using TUnit.Core.Executors;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6885;

/// <summary>
/// Regression test for https://github.com/thomhurst/TUnit/issues/6885
/// A test run by a <see cref="DedicatedThreadExecutor"/> must not complete until the executor's
/// CleanUp() has returned, so the next test in a [NotInParallel] class cannot overlap it.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel]
[TestExecutor<SlowCleanUpExecutor>]
public class DedicatedThreadExecutorCleanUpOrderTests
{
    [Test]
    public Task Test1() => AssertNoOverlapAsync();

    [Test]
    public Task Test2() => AssertNoOverlapAsync();

    [Test]
    public Task Test3() => AssertNoOverlapAsync();

    private static async Task AssertNoOverlapAsync()
    {
        var cleanUpRunningAtStart = SlowCleanUpExecutor.CleanUpRunning;

        // Longer than CleanUp(), so a previous test's late CleanUp() would reset the state mid-test.
        await Task.Delay(400);

        using (Assert.Multiple())
        {
            await Assert.That(cleanUpRunningAtStart).IsFalse();
            await Assert.That(SlowCleanUpExecutor.State).IsEqualTo("configured");
        }
    }
}

/// <summary>
/// Sets global state in Initialize() and resets it in a slow CleanUp().
/// </summary>
public sealed class SlowCleanUpExecutor : DedicatedThreadExecutor
{
    private static volatile bool s_cleanUpRunning;
    private static volatile string s_state = "reset";

    public static bool CleanUpRunning => s_cleanUpRunning;

    public static string State => s_state;

    protected override void Initialize()
    {
        s_state = "configured";
    }

    protected override void CleanUp()
    {
        s_cleanUpRunning = true;
        Thread.Sleep(300);
        s_state = "reset";
        s_cleanUpRunning = false;
    }
}
