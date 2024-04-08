using TUnit.Core.Exceptions;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class SkipAttribute : TUnitAttribute, IApplicableTestAttribute
{
    public string Reason { get; }

    public SkipAttribute(string reason)
    {
        Reason = reason;
    }

    public async Task Apply(TestContext testContext)
    {
        await Task.CompletedTask;
     
        if (ShouldSkipPredicate(testContext))
        {
            throw new SkipTestException(Reason);
        }
    }

    public virtual Func<TestContext, bool> ShouldSkipPredicate { get; } = _ => true;
}