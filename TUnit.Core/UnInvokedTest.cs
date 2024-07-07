using System.Reflection;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class UnInvokedTest<TTestClass> : UnInvokedTest
{
    private readonly ResettableLazy<TTestClass> _resettableLazyTestClassFactory;

    public UnInvokedTest(ResettableLazy<TTestClass> resettableLazyTestClassFactory)
    {
        _resettableLazyTestClassFactory = resettableLazyTestClassFactory;
    }
    
    public required List<InstanceMethod<TTestClass>> BeforeEachTestSetUps { get; init; }

    public TTestClass TestClass => _resettableLazyTestClassFactory.Value;
    
    public required Func<TTestClass, CancellationToken, Task> TestBody { get; init; }
    
    public required List<InstanceMethod<TTestClass>> AfterEachTestCleanUps { get; init; }
    
    
    public override async Task RunBeforeEachTestSetUps(CancellationToken engineToken)
    {
        foreach (var setUp in BeforeEachTestSetUps)
        {
            var timeout = setUp.MethodInfo.GetCustomAttributes().OfType<TimeoutAttribute>().FirstOrDefault()?.Timeout;
            var token = CancellationTokenSource.CreateLinkedTokenSource(engineToken);

            if (timeout != null)
            {
                token.CancelAfter(timeout.Value);
            }
            
            await setUp.Body(TestClass, token.Token);
        }
    }

    public override async Task ExecuteTest(CancellationToken cancellationToken)
    {
        await TestBody.Invoke(TestClass, cancellationToken);
    }

    public override async Task RunAfterEachTestCleanUps(List<Exception> exceptionsTracker, CancellationToken engineToken)
    {
        foreach (var cleanUp in AfterEachTestCleanUps)
        {
            var timeout = cleanUp.MethodInfo.GetCustomAttributes().OfType<TimeoutAttribute>().FirstOrDefault()?.Timeout;
            var token = CancellationTokenSource.CreateLinkedTokenSource(engineToken);

            if (timeout != null)
            {
                token.CancelAfter(timeout.Value);
            }
            
            await RunHelpers.RunSafelyAsync(() => cleanUp.Body(TestClass, token.Token), exceptionsTracker);
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

    public abstract Task RunBeforeEachTestSetUps(CancellationToken engineToken);
    public abstract Task ExecuteTest(CancellationToken cancellationToken);
    public abstract Task RunAfterEachTestCleanUps(List<Exception> exceptionsTracker, CancellationToken engineToken);
    public abstract void ResetTestInstance();
}