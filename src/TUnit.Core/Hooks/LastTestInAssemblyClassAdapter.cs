using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Hooks;

public class LastTestInAssemblyAdapter(ILastTestInAssemblyEventReceiver lastTestInAssemblyEventReceiver, TestContext testContext) : IExecutableHook<AssemblyHookContext>
{
    public string Name => nameof(lastTestInAssemblyEventReceiver.OnLastTestInAssembly);

    [field: AllowNull, MaybeNull]
    public MethodMetadata MethodInfo => field ??= new MethodMetadata
    {
        Type = typeof(ILastTestInAssemblyEventReceiver),
        TypeInfo = new ConcreteType(typeof(ILastTestInAssemblyEventReceiver)),
        Name = nameof(lastTestInAssemblyEventReceiver.OnLastTestInAssembly),
        Parameters = [new ParameterMetadata<AssemblyHookContext>
        {
            TypeInfo = new ConcreteType(typeof(AssemblyHookContext)),
            Name = "context",
            ReflectionInfo = typeof(ILastTestInAssemblyEventReceiver).GetMethod(nameof(ILastTestInAssemblyEventReceiver.OnLastTestInAssembly))!.GetParameters()[0],
        }, new ParameterMetadata<TestContext>
        {
            TypeInfo = new ConcreteType(typeof(TestContext)),
            Name = "testContext",
            ReflectionInfo = typeof(ILastTestInAssemblyEventReceiver).GetMethod(nameof(ILastTestInAssemblyEventReceiver.OnLastTestInAssembly))!.GetParameters()[1],
        }],
        GenericTypeCount = 0,
        ReturnType = typeof(ValueTask),
        ReturnTypeInfo = new ConcreteType(typeof(ValueTask)),
        Class = new ClassMetadata
        {
            Parent = null,
            Type = typeof(ILastTestInAssemblyEventReceiver),
            TypeInfo = new ConcreteType(typeof(ILastTestInAssemblyEventReceiver)),
            Assembly = new AssemblyMetadata
            {
                Name = "TUnit.Core",
            },
            Namespace = "TUnit.Core.Interfaces",
            Name = "ILastTestInAssemblyEventReceiver",
            Parameters = [],
            Properties = [],
        }
    };

    public int Order => 0;

    public bool Execute(AssemblyHookContext context, CancellationToken cancellationToken)
    {
        return false;
    }

    public async ValueTask ExecuteAsync(AssemblyHookContext context, CancellationToken cancellationToken)
    {
        await lastTestInAssemblyEventReceiver.OnLastTestInAssembly(context, testContext);
    }
}
