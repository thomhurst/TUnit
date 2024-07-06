using TUnit.Core.Exceptions;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class SkipAttribute : TUnitAttribute, IBeforeTestAttribute
{
    public string Reason { get; }

    public SkipAttribute(string reason)
    {
        Reason = reason;
    }

    public Task OnBeforeTest(TestContext testContext)
    {
        if (ShouldSkipPredicate(testContext))
        {
            throw new SkipTestException(Reason);
        }

        return Task.CompletedTask;
    }

    public virtual Func<TestContext, bool> ShouldSkipPredicate { get; } = _ => true;
}