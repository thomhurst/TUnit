using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// A discovered test that uses a provided method invoker instead of reflection.
/// </summary>
internal record UnifiedDiscoveredTest(
    ResettableLazy<object> ResettableLazy,
    Func<object, CancellationToken, ValueTask> TestMethodInvoker) : DiscoveredTest
{
    public override async ValueTask ExecuteTest(CancellationToken cancellationToken)
    {
        TestContext.CancellationToken = cancellationToken;
        
        var classInstance = ResettableLazy.Value;
        
        await TestExecutor.ExecuteTest(TestContext, async () =>
        {
            await TestMethodInvoker(classInstance, cancellationToken);
        });
    }

    public override ValueTask ResetTestInstance()
    {
        return ResettableLazy.ResetLazy();
    }

    public override IClassConstructor? ClassConstructor => ResettableLazy.ClassConstructor;
}