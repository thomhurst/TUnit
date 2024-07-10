using System.Reflection;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

internal class DiscoveredTest<TTestClass> : DiscoveredTest
{
    private readonly ResettableLazy<TTestClass> _resettableLazyTestClassFactory;

    public DiscoveredTest(ResettableLazy<TTestClass> resettableLazyTestClassFactory)
    {
        _resettableLazyTestClassFactory = resettableLazyTestClassFactory;
    }
    
    public required List<InstanceMethod<TTestClass>> BeforeEachTestSetUps { get; init; }

    public TTestClass TestClass => _resettableLazyTestClassFactory.Value;
    
    public required Func<TTestClass, CancellationToken, Task> TestBody { get; init; }
    
    public required List<InstanceMethod<TTestClass>> AfterEachTestCleanUps { get; init; }

    public override async Task ExecuteTest(CancellationToken cancellationToken)
    {
        await TestBody.Invoke(TestClass, cancellationToken);
    }

    public override IEnumerable<Func<Task>> GetSetUps(CancellationToken engineToken)
    {
        foreach (var setUp in BeforeEachTestSetUps)
        {
            var timeout = setUp.MethodInfo.GetCustomAttributes().OfType<TimeoutAttribute>().FirstOrDefault()?.Timeout;
            var token = engineToken ;

            yield return () =>
            {
                if (timeout != null)
                {
                    var cts = CancellationTokenSource.CreateLinkedTokenSource(engineToken);
                    token = cts.Token;
                    cts.CancelAfter(timeout.Value);
                }
                
                return setUp.Body.Invoke(TestClass, TestContext, token);
            };
        }
    }

    public override IEnumerable<Func<Task>> GetCleanUps(CancellationToken engineToken)
    {
        foreach (var cleanUp in AfterEachTestCleanUps)
        {
            var timeout = cleanUp.MethodInfo.GetCustomAttributes().OfType<TimeoutAttribute>().FirstOrDefault()?.Timeout;
            var token = engineToken ;
            
            yield return () =>
            {
                if (timeout != null)
                {
                    var cts = CancellationTokenSource.CreateLinkedTokenSource(engineToken);
                    token = cts.Token;
                    cts.CancelAfter(timeout.Value);
                }

                return cleanUp.Body.Invoke(TestClass, TestContext, token);
            };
        }
    }

    public override void ResetTestInstance()
    {
        _resettableLazyTestClassFactory.ResetLazy();
    }
}

internal abstract class DiscoveredTest
{
    public required TestContext TestContext { get; init; }
    
    public required IBeforeTestAttribute[] BeforeTestAttributes { get; init; }
    public required IAfterTestAttribute[] AfterTestAttributes { get; init; }

    public abstract Task ExecuteTest(CancellationToken cancellationToken);
    public abstract IEnumerable<Func<Task>> GetSetUps(CancellationToken engineToken);
    public abstract IEnumerable<Func<Task>> GetCleanUps(CancellationToken engineToken);

    public abstract void ResetTestInstance();
    
    public TestDetails TestDetails => TestContext.TestDetails;
}