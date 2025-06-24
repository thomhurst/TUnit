using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Executors;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class TestExecutorAttribute<T> : TUnitAttribute, ITestRegisteredEventReceiver where T : ITestExecutor, new()
{
    public int Order => 0;

    public ValueTask OnTestRegistered(TestContext context)
    {
        context.InternalDiscoveredTest!.TestExecutor = new T();
        return default;
    }
}

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class TestExecutorAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type) : TUnitAttribute, ITestRegisteredEventReceiver
{
    public int Order => 0;

    public ValueTask OnTestRegistered(TestContext context)
    {
        context.InternalDiscoveredTest!.TestExecutor = (ITestExecutor)Activator.CreateInstance(type)!;
        return default;
    }
}