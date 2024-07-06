using TUnit.Core.Helpers;
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
    
    public required Func<TTestClass, Task> TestBody { get; init; }
    
    public required List<Func<TTestClass, Task>> AfterEachTestCleanUps { get; init; }
    
    
    public override async Task RunBeforeEachTestSetUps()
    {
        foreach (var setUp in BeforeEachTestSetUps)
        {
            await setUp(TestClass);
        }
    }

    public override async Task ExecuteTest()
    {
        await TestBody.Invoke(TestClass);
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
    
    public required List<IBeforeTestAttribute> BeforeTestAttributes { get; init; }
    public required List<IAfterTestAttribute> AfterTestAttributes { get; init; }

    public abstract Task RunBeforeEachTestSetUps();
    public abstract Task ExecuteTest();
    public abstract Task RunAfterEachTestCleanUps(List<Exception> exceptionsTracker);
    public abstract void ResetTestInstance();
}