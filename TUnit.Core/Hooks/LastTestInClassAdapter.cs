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
        Attributes = [],
        Name = nameof(lastTestInClassEventReceiver.OnLastTestInClass),
        Parameters = [new ParameterMetadata<ClassHookContext>
        {
            Attributes = [],
            Name = "context",
            ReflectionInfo = typeof(ILastTestInClassEventReceiver).GetMethod(nameof(ILastTestInClassEventReceiver.OnLastTestInClass))!.GetParameters()[0],
        }, new ParameterMetadata<TestContext>
        {
            Attributes = [],
            Name = "testContext",
            ReflectionInfo = typeof(ILastTestInClassEventReceiver).GetMethod(nameof(ILastTestInClassEventReceiver.OnLastTestInClass))!.GetParameters()[0],
        }],
        GenericTypeCount = 0,
        ReturnType = typeof(ValueTask),
        Class = new ClassMetadata
        {
            Parent = null,
            Type = typeof(ILastTestInClassEventReceiver),
            Assembly = new AssemblyMetadata
            {
                Name = "TUnit.Core",
                Attributes = [],
            },
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
