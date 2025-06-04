using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Hooks;

public class LastTestInClassAdapter(ILastTestInClassEventReceiver lastTestInClassEventReceiver, TestContext testContext) : IExecutableHook<ClassHookContext>
{
    public string Name => nameof(lastTestInClassEventReceiver.OnLastTestInClass);

    [field: AllowNull, MaybeNull]
    public SourceGeneratedMethodInformation MethodInfo => field ??= new SourceGeneratedMethodInformation
    {
        Type = typeof(ILastTestInClassEventReceiver),
        Attributes = [],
        Name = nameof(lastTestInClassEventReceiver.OnLastTestInClass),
        Parameters = [new SourceGeneratedParameterInformation<ClassHookContext>
        {
            Attributes = [],
            Name = "context",
            IsOptional = false,
            DefaultValue = null
        }, new SourceGeneratedParameterInformation<TestContext>
        {
            Attributes = [],
            Name = "testContext",
            IsOptional = false,
            DefaultValue = null
        }],
        GenericTypeCount = 0,
        ReturnType = typeof(ValueTask),
        Class = new SourceGeneratedClassInformation
        {
            Parent = null,
            Type = typeof(ILastTestInClassEventReceiver),
            Assembly = new SourceGeneratedAssemblyInformation
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
