using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Hooks;

public class LastTestInClassAdapter(ILastTestInClassEventReceiver lastTestInClassEventReceiver, TestContext testContext) : IExecutableHook<ClassHookContext>
{
    public string Name => nameof(lastTestInClassEventReceiver.OnLastTestInClass);

    [field: AllowNull, MaybeNull]
    public MethodMetadata MethodInfo => field ??= new MethodMetadata
    {
        Type = typeof(ILastTestInClassEventReceiver),
        TypeReference = TypeReference.CreateConcrete(typeof(ILastTestInClassEventReceiver).AssemblyQualifiedName!),
        Attributes = [],
        Name = nameof(lastTestInClassEventReceiver.OnLastTestInClass),
        Parameters = [new ParameterMetadata<ClassHookContext>
        {
            TypeReference = TypeReference.CreateConcrete(typeof(ClassHookContext).AssemblyQualifiedName!),
            Attributes = [],
            Name = "context",
            ReflectionInfo = typeof(ILastTestInClassEventReceiver).GetMethod(nameof(ILastTestInClassEventReceiver.OnLastTestInClass))!.GetParameters()[0],
        }, new ParameterMetadata<TestContext>
        {
            TypeReference = TypeReference.CreateConcrete(typeof(TestContext).AssemblyQualifiedName!),
            Attributes = [],
            Name = "testContext",
            ReflectionInfo = typeof(ILastTestInClassEventReceiver).GetMethod(nameof(ILastTestInClassEventReceiver.OnLastTestInClass))!.GetParameters()[0],
        }],
        GenericTypeCount = 0,
        ReturnType = typeof(ValueTask),
        ReturnTypeReference = TypeReference.CreateConcrete(typeof(ValueTask).AssemblyQualifiedName!),
        Class = new ClassMetadata
        {
            Parent = null,
            Type = typeof(ILastTestInClassEventReceiver),
            TypeReference = TypeReference.CreateConcrete(typeof(ILastTestInClassEventReceiver).AssemblyQualifiedName!),
            Assembly = new AssemblyMetadata
            {
                Name = "TUnit.Core",
                Attributes = [],
            },
            Constructors = [],
            Attributes = [],
            Namespace = "TUnit.Core.Interfaces",
            Name = "ILastTestInClassEventReceiver",
            Parameters = [],
            Properties = [],
        }
    };

    public int Order => 0;

    public bool Execute(ClassHookContext context, CancellationToken cancellationToken)
    {
        return false;
    }

    public async ValueTask ExecuteAsync(ClassHookContext context, CancellationToken cancellationToken)
    {
        await lastTestInClassEventReceiver.OnLastTestInClass(context, testContext);
    }
}
