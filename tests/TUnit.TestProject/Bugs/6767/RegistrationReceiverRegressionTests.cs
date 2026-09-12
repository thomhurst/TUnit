using TUnit.Core.Executors;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6767;

[EngineTest(ExpectedResult.Pass)]
public class ExecutorReceiverOverlapTests
{
    [Test]
    [EligibleExecutor]
    [InstallEligibleExecutor(-100)]
    public async Task InstalledBeforeOriginalReceiver()
    {
        await Assert.That(TestContext.Current!.StateBag.Items["EligibleRegistrationCount"]).IsEqualTo(1);
        await Assert.That((bool)TestContext.Current.StateBag.Items["AlreadyRegisteredAtInstallation"]!).IsFalse();
    }

    [Test]
    [EligibleExecutor]
    [InstallEligibleExecutor(100)]
    public async Task InstalledAfterOriginalReceiver()
    {
        await Assert.That(TestContext.Current!.StateBag.Items["EligibleRegistrationCount"]).IsEqualTo(1);
        await Assert.That((bool)TestContext.Current.StateBag.Items["AlreadyRegisteredAtInstallation"]!).IsTrue();
    }
}

internal sealed class EligibleExecutorAttribute : Attribute, ITestExecutor, ITestRegisteredEventReceiver
{
    public int Order => 0;

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.StateBag.AddOrUpdate("EligibleRegistrationCount", 1, static (_, count) => (int)count! + 1);
        return default;
    }

    public ValueTask ExecuteTest(TestContext context, Func<ValueTask> action) => action();
}

internal sealed class InstallEligibleExecutorAttribute(int order) : Attribute, ITestRegisteredEventReceiver
{
    public int Order => order;

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        var executor = context.TestDetails.GetAllAttributes().OfType<EligibleExecutorAttribute>().Single();
        context.StateBag["AlreadyRegisteredAtInstallation"] = context.StateBag.ContainsKey("EligibleRegistrationCount");
        context.SetTestExecutor(executor);
        return default;
    }
}

[EngineTest(ExpectedResult.Pass)]
public class SelfInstallingExecutorTests
{
    [Test]
    [SelfInstallingExecutor]
    public async Task AttributeInstallsItselfOnlyOnce()
    {
        await Assert.That(TestContext.Current!.StateBag.Items["SelfRegistrationCount"]).IsEqualTo(1);
    }
}

internal sealed class SelfInstallingExecutorAttribute : Attribute, ITestExecutor, ITestRegisteredEventReceiver
{
    public int Order => 0;

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.StateBag.AddOrUpdate("SelfRegistrationCount", 1, static (_, count) => (int)count! + 1);
        context.SetTestExecutor(this);
        return default;
    }

    public ValueTask ExecuteTest(TestContext context, Func<ValueTask> action) => action();
}

[EngineTest(ExpectedResult.Pass)]
[TestExecutor<WideExecutor>]
public class ClassExecutorLimiterPrecedenceTests
{
    [Test]
    [ParallelLimiter<SerialLimit>]
    public async Task MethodLimiterOverridesClassExecutor()
    {
        await Assert.That(TestContext.Current!.Parallelism.Limiter).IsTypeOf<SerialLimit>();
        await Assert.That((bool)TestContext.Current.StateBag.Items["ExecutorSawExplicitLimiter"]!).IsTrue();
    }

    [Test]
    public async Task ExecutorLimiterAppliesWithoutExplicitAttribute()
    {
        await Assert.That(TestContext.Current!.Parallelism.Limiter).IsTypeOf<WideLimit>();
        await Assert.That(TestContext.Current.StateBag.Items["WideExecutorRegistrationCount"]).IsEqualTo(1);
    }

    [Test]
    [TestExecutor<SerialExecutor>]
    [ParallelLimiter<WideLimit>]
    public async Task ExplicitLimiterCanIncreaseExecutorLimit()
    {
        await Assert.That(TestContext.Current!.Parallelism.Limiter).IsTypeOf<WideLimit>();
    }
}

[EngineTest(ExpectedResult.Pass)]
public class ExplicitLimiterPrecedenceTests
{
    [Test]
    [ParallelLimiter<SerialLimit>]
    [SetWideLimiter(-100, false)]
    public Task ProgrammaticLimiterBeforeAttribute() => AssertExplicitLimiter(false, false);

    [Test]
    [ParallelLimiter<SerialLimit>]
    [SetWideLimiter(100, false)]
    public Task ProgrammaticLimiterAfterAttribute() => AssertExplicitLimiter(true, false);

    [Test]
    [ParallelLimiter<SerialLimit>]
    [SetWideLimiter(-100, true)]
    public Task ExecutorBeforeAttribute() => AssertExplicitLimiter(false, true);

    [Test]
    [ParallelLimiter<SerialLimit>]
    [SetWideLimiter(100, true)]
    public Task ExecutorAfterAttribute() => AssertExplicitLimiter(true, true);

    private static async Task AssertExplicitLimiter(bool explicitLimiterWasAlreadySet, bool executorInstalled)
    {
        var context = TestContext.Current!;
        await Assert.That(context.Parallelism.Limiter).IsTypeOf<SerialLimit>();
        await Assert.That(context.StateBag.Items["ProgrammaticReceiverSawExplicitLimiter"])
            .IsEqualTo(explicitLimiterWasAlreadySet);
        if (executorInstalled)
        {
            await Assert.That(context.StateBag.Items["ExecutorSawExplicitLimiter"]).IsEqualTo(explicitLimiterWasAlreadySet);
            await Assert.That(context.StateBag.Items["WideExecutorRegistrationCount"]).IsEqualTo(1);
        }
    }
}

internal sealed class SetWideLimiterAttribute(int order, bool installExecutor) : Attribute, ITestRegisteredEventReceiver
{
    public int Order => order;

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.StateBag["ProgrammaticReceiverSawExplicitLimiter"] = context.TestContext.Parallelism.Limiter is SerialLimit;
        if (installExecutor)
        {
            var executor = new WideExecutor();
            context.SetTestExecutor(executor);
            context.SetHookExecutor(executor);
        }
        else
        {
            context.SetParallelLimiter(new WideLimit());
        }

        return default;
    }
}

internal sealed class WideExecutor : GenericAbstractExecutor, ITestRegisteredEventReceiver
{
    // Dynamic dispatch follows the installer, even when this Order would place it later.
    int IEventReceiver.Order => 1000;

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.StateBag["ExecutorSawExplicitLimiter"] = context.TestContext.Parallelism.Limiter is SerialLimit;
        context.StateBag.AddOrUpdate("WideExecutorRegistrationCount", 1, static (_, count) => (int)count! + 1);
        context.SetParallelLimiter(new WideLimit());
        return default;
    }

    protected override ValueTask ExecuteAsync(Func<ValueTask> action) => action();
}

internal sealed class WideLimit : IParallelLimit
{
    public int Limit => 8;
}
