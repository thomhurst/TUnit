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
        TypeReference = TypeReference.CreateConcrete(typeof(ILastTestInAssemblyEventReceiver).AssemblyQualifiedName!),
        Attributes = [],
        Name = nameof(lastTestInAssemblyEventReceiver.OnLastTestInAssembly),
        Parameters = [new ParameterMetadata<AssemblyHookContext>
        {
            TypeReference = TypeReference.CreateConcrete(typeof(AssemblyHookContext).AssemblyQualifiedName!),
            Attributes = [],
            Name = "context",
            ReflectionInfo = typeof(ILastTestInAssemblyEventReceiver).GetMethod(nameof(ILastTestInAssemblyEventReceiver.OnLastTestInAssembly))!.GetParameters()[0],
        }, new ParameterMetadata<TestContext>
        {
            TypeReference = TypeReference.CreateConcrete(typeof(TestContext).AssemblyQualifiedName!),
            Attributes = [],
            Name = "testContext",
            ReflectionInfo = typeof(ILastTestInAssemblyEventReceiver).GetMethod(nameof(ILastTestInAssemblyEventReceiver.OnLastTestInAssembly))!.GetParameters()[1],
        }],
        GenericTypeCount = 0,
        ReturnType = typeof(ValueTask),
        ReturnTypeReference = TypeReference.CreateConcrete(typeof(ValueTask).AssemblyQualifiedName!),
        Class = new ClassMetadata
        {
            Parent = null,
            Type = typeof(ILastTestInAssemblyEventReceiver),
            TypeReference = TypeReference.CreateConcrete(typeof(ILastTestInAssemblyEventReceiver).AssemblyQualifiedName!),
            Assembly = new AssemblyMetadata
            {
                Name = "TUnit.Core",
                Attributes = [],
            },
            Attributes = [],
            Constructors = [],
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
