using TUnit.Core.Exceptions;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class SkipAttribute(string reason) : TUnitAttribute, ITestStartEvent
{
    public string Reason { get; protected set; } = reason;

    public async ValueTask OnTestStart(BeforeTestContext context)
    {
        if (await ShouldSkip(context))
        {
            throw new SkipTestException(Reason);
        }
    }

    public virtual Task<bool> ShouldSkip(BeforeTestContext context) => Task.FromResult(true);
}