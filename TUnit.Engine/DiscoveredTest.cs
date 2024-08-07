using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Engine;

internal class DiscoveredTest<TTestClass> : DiscoveredTest
{
    private readonly ResettableLazy<TTestClass> _resettableLazyTestClassFactory;

    public DiscoveredTest(ResettableLazy<TTestClass> resettableLazyTestClassFactory)
    {
        _resettableLazyTestClassFactory = resettableLazyTestClassFactory;
    }
    
    public TTestClass TestClass => _resettableLazyTestClassFactory.Value;
    
    public required Func<TTestClass, CancellationToken, Task> TestBody { get; init; }
    
    public override async Task ExecuteTest(CancellationToken cancellationToken)
    {
        await TestBody.Invoke(TestClass, cancellationToken);
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

    public abstract void ResetTestInstance();
    
    public TestDetails TestDetails => TestContext.TestDetails;
}