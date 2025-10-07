using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Hooks;

#if NET6_0_OR_GREATER
[RequiresUnreferencedCode("Method with DynamicallyAccessedMembersAttribute accessed via reflection")]
#endif
public class LastTestInClassAdapter(ILastTestInClassEventReceiver lastTestInClassEventReceiver, TestContext testContext) : IExecutableHook<ClassHookContext>
{
    public string Name => nameof(lastTestInClassEventReceiver.OnLastTestInClass);

    [field: AllowNull, MaybeNull]
    public MethodMetadata MethodInfo => field ??= new MethodMetadata
    {
        Type = typeof(ILastTestInClassEventReceiver),
        TypeInfo = new ConcreteType(typeof(ILastTestInClassEventReceiver)),
        Name = nameof(lastTestInClassEventReceiver.OnLastTestInClass),
        Parameters = [new ParameterMetadata<ClassHookContext>
        {
            TypeInfo = new ConcreteType(typeof(ClassHookContext)),
            Name = "context",
            ReflectionInfo = typeof(ILastTestInClassEventReceiver).GetMethod(nameof(ILastTestInClassEventReceiver.OnLastTestInClass))!.GetParameters()[0],
        }, new ParameterMetadata<TestContext>
        {
            TypeInfo = new ConcreteType(typeof(TestContext)),
            Name = "testContext",
            ReflectionInfo = typeof(ILastTestInClassEventReceiver).GetMethod(nameof(ILastTestInClassEventReceiver.OnLastTestInClass))!.GetParameters()[0],
        }],
        GenericTypeCount = 0,
        ReturnType = typeof(ValueTask),
        ReturnTypeInfo = new ConcreteType(typeof(ValueTask)),
        Class = new ClassMetadata
        {
            Parent = null,
            Type = typeof(ILastTestInClassEventReceiver),
            TypeInfo = new ConcreteType(typeof(ILastTestInClassEventReceiver)),
            Assembly = new AssemblyMetadata
            {
                Name = "TUnit.Core",
            },
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
