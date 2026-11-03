using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7391;

public sealed class FailingInjectionArgument : ITestEndEventReceiver, IAsyncDisposable
{
    public static bool EndRan { get; private set; }
    public static bool DisposedBeforeEnd { get; private set; }
    public static bool Disposed { get; private set; }

    [ClassDataSource<ThrowingResource>]
    public required ThrowingResource Bad { get; init; }

    public int Order => 0;

    public ValueTask OnTestEnd(TestContext context)
    {
        EndRan = true;
        DisposedBeforeEnd = Disposed;
        return default;
    }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return default;
    }
}

[EngineTest(ExpectedResult.Failure)]
public sealed class ArgumentInjectionFailureTests
{
    [Test]
    [NotInParallel("7391-argument", Order = 1)]
    [ClassDataSource<FailingInjectionArgument>(Shared = SharedType.Keyed, Key = "7391-argument", DeferEnumeration = true)]
    public void Fails_During_Argument_Injection(FailingInjectionArgument argument)
    {
    }
}

[EngineTest(ExpectedResult.Failure)]
public sealed class ArgumentInjectionFailureObserverTests
{
    [Test]
    [DependsOn(typeof(ArgumentInjectionFailureTests), nameof(ArgumentInjectionFailureTests.Fails_During_Argument_Injection), ProceedOnFailure = true)]
    public async Task Argument_Lives_Until_The_Failed_Test_Ends()
    {
        await Assert.That(FailingInjectionArgument.EndRan).IsTrue();
        await Assert.That(FailingInjectionArgument.DisposedBeforeEnd).IsFalse();
        await Assert.That(FailingInjectionArgument.Disposed).IsTrue();
    }
}
