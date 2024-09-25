using TUnit.Core.Exceptions;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class SkipAttribute : TUnitAttribute, IBeforeTestAttribute
{
    public string Reason { get; protected set; }

    public SkipAttribute(string reason)
    {
        Reason = reason;
    }

    public async Task OnBeforeTest(BeforeTestContext context)
    {
        if (await ShouldSkip(context))
        {
            throw new SkipTestException(Reason);
        }
    }

    public virtual Task<bool> ShouldSkip(BeforeTestContext context) => Task.FromResult(true);
}