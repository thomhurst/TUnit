using TUnit.Core.Executors;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6767;

/// <summary>
/// Regression test for https://github.com/thomhurst/TUnit/issues/6767
/// An executor that also implements ITestRegisteredEventReceiver must receive
/// OnTestRegistered, so the parallel limit it declares during registration is applied.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[TestExecutor<SerialExecutor>]
public class ExecutorEventReceiverTests
{
    private static int s_concurrent;
    private static int s_peak;

    [Before(Class)]
    public static void Reset()
    {
        s_concurrent = 0;
        s_peak = 0;
    }

    [Test]
    public async Task ExecutorSetsTheLimiter()
    {
        await Assert.That(TestContext.Current!.Parallelism.Limiter).IsTypeOf<SerialLimit>();
    }

    [Test, Repeat(9)]
    public Task Measure() => MeasureAsync();

    [Test, DependsOn(nameof(Measure))]
    public async Task PeakIsOne()
    {
        await Assert.That(s_peak).IsEqualTo(1);
    }

    private static async Task MeasureAsync()
    {
        var current = Interlocked.Increment(ref s_concurrent);

        int old;
        do
        {
            old = s_peak;
            if (current <= old)
            {
                break;
            }
        }
        while (Interlocked.CompareExchange(ref s_peak, current, old) != old);

        await Task.Delay(50).ConfigureAwait(false);

        Interlocked.Decrement(ref s_concurrent);
    }
}

/// <summary>
/// An executor is installed by an attribute part-way through registration, so it can only
/// declare its parallel limit through its own ITestRegisteredEventReceiver implementation.
/// </summary>
internal sealed class SerialExecutor : GenericAbstractExecutor, ITestRegisteredEventReceiver
{
    public int Order => 0;

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.SetParallelLimiter(new SerialLimit());
        return default;
    }

    protected override ValueTask ExecuteAsync(Func<ValueTask> action) => action();
}

internal sealed class SerialLimit : IParallelLimit
{
    public int Limit => 1;
}

/// <summary>
/// Two distinct executors that compare equal each own their registration callback, so the queue
/// behind that dispatch has to hold them apart by reference and not by equality.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[TwoEqualExecutors]
public class EqualExecutorInstanceTests
{
    [Test]
    public async Task BothInstancesAreRegistered()
    {
        using (Assert.Multiple())
        {
            await Assert.That(TwoEqualExecutorsAttribute.TestExecutor!.IsRegistered).IsTrue();
            await Assert.That(TwoEqualExecutorsAttribute.HookExecutor!.IsRegistered).IsTrue();
        }
    }
}

internal sealed class TwoEqualExecutorsAttribute : Attribute, ITestRegisteredEventReceiver
{
    public static EqualByTypeExecutor? TestExecutor { get; private set; }
    public static EqualByTypeExecutor? HookExecutor { get; private set; }

    public int Order => 0;

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        var testExecutor = new EqualByTypeExecutor();
        var hookExecutor = new EqualByTypeExecutor();

        context.SetTestExecutor(testExecutor);
        context.SetHookExecutor(hookExecutor);

        TestExecutor = testExecutor;
        HookExecutor = hookExecutor;

        return default;
    }
}

/// <summary>
/// Every instance compares equal to every other, so a queue keyed on equality would drop the second.
/// </summary>
internal sealed class EqualByTypeExecutor : GenericAbstractExecutor, ITestRegisteredEventReceiver
{
    public bool IsRegistered { get; private set; }

    public int Order => 0;

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        IsRegistered = true;
        return default;
    }

    public override bool Equals(object? obj) => obj is EqualByTypeExecutor;

    public override int GetHashCode() => nameof(EqualByTypeExecutor).GetHashCode();

    protected override ValueTask ExecuteAsync(Func<ValueTask> action) => action();
}
