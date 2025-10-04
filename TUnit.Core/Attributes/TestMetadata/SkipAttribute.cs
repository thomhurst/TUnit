using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class SkipAttribute : Attribute, ITestRegisteredEventReceiver
{
    public string Reason { get; }

    public SkipAttribute(string reason)
    {
        Reason = reason;
    }

    /// <inheritdoc />
    public int Order => int.MinValue;

    /// <inheritdoc />
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Type comes from runtime objects that cannot be annotated")]
#endif
    public async ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        if (await ShouldSkip(context))
        {
            // Store skip reason directly on TestContext
            context.TestContext.SkipReason = Reason;
            context.TestContext.TestDetails.ClassInstance = SkippedTestInstance.Instance;
        }
    }

    public virtual Task<bool> ShouldSkip(TestRegisteredContext context) => Task.FromResult(true);
}
