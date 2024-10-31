using TUnit.Core.Exceptions;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class SkipAttribute(string reason) : TUnitAttribute, ITestRegisteredEventReceiver
{
    public string Reason { get; protected set; } = reason;

    public int Order => int.MinValue;
    public async ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        if (await ShouldSkip(context))
        {
            context.SkipTest(Reason);
        }
    }

    public virtual Task<bool> ShouldSkip(BeforeTestContext context) => Task.FromResult(true);
}