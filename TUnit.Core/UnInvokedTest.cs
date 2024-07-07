using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class UnInvokedTest<TTestClass> : UnInvokedTest
{
    private readonly ResettableLazy<TTestClass> _resettableLazyTestClassFactory;

    public UnInvokedTest(ResettableLazy<TTestClass> resettableLazyTestClassFactory)
    {
        _resettableLazyTestClassFactory = resettableLazyTestClassFactory;
    }
    
    public required List<Func<TTestClass, Task>> BeforeEachTestSetUps { get; init; }

    public TTestClass TestClass => _resettableLazyTestClassFactory.Value;
    
    public required Func<TTestClass, CancellationToken, Task> TestBody { get; init; }
    
    public required List<Func<TTestClass, Task>> AfterEachTestCleanUps { get; init; }
    
    
    public override async Task RunBeforeEachTestSetUps()
    {
        foreach (var setUp in BeforeEachTestSetUps)
        {
            await setUp(TestClass);
        }
    }

    public override async Task ExecuteTest(CancellationToken cancellationToken)
    {
        await TestBody.Invoke(TestClass, cancellationToken);
    }

    public override async Task RunAfterEachTestCleanUps(List<Exception> exceptionsTracker)
    {
        foreach (var cleanUp in AfterEachTestCleanUps)
        {
            await RunHelpers.RunSafelyAsync(() => cleanUp(TestClass), exceptionsTracker);
        }
        
        await RunHelpers.RunSafelyAsync(() => RunHelpers.Dispose(TestClass), exceptionsTracker);
    }

    public override void ResetTestInstance()
    {
        _resettableLazyTestClassFactory.ResetLazy();
    }
}

public abstract class UnInvokedTest
{
    public required string Id { get; init; }
    public required TestContext TestContext { get; init; }
    
    public required IBeforeTestAttribute[] BeforeTestAttributes { get; init; }
    public required IAfterTestAttribute[] AfterTestAttributes { get; init; }

    public abstract Task RunBeforeEachTestSetUps();
    public abstract Task ExecuteTest(CancellationToken cancellationToken);
    public abstract Task RunAfterEachTestCleanUps(List<Exception> exceptionsTracker);
    public abstract void ResetTestInstance();
}