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

    public async Task OnBeforeTest(TestContext testContext)
    {
        if (await ShouldSkip(testContext))
        {
            throw new SkipTestException(Reason);
        }
    }

    public virtual Task<bool> ShouldSkip(TestContext testContext) => Task.FromResult(true);
}