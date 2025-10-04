using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Executors;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class TestExecutorAttribute<T> : TUnitAttribute, ITestRegisteredEventReceiver where T : ITestExecutor, new()
{
    public int Order => 0;

#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Type comes from runtime objects that cannot be annotated")]
#endif
    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.SetTestExecutor(new T());
        return default(ValueTask);
    }
}

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class TestExecutorAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type) : TUnitAttribute, ITestRegisteredEventReceiver
{
    public int Order => 0;

#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Type comes from runtime objects that cannot be annotated")]
#endif
    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.SetTestExecutor((ITestExecutor) Activator.CreateInstance(type)!);
        return default(ValueTask);
    }
}
