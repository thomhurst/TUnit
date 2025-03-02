using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Hooks;

public class LastTestInAssemblyAdapter(ILastTestInAssemblyEventReceiver lastTestInAssemblyEventReceiver, TestContext testContext) : IExecutableHook<AssemblyHookContext>
{
    public string Name => nameof(lastTestInAssemblyEventReceiver.OnLastTestInAssembly);

    [field: AllowNull, MaybeNull]
    public SourceGeneratedMethodInformation MethodInfo => field ??= new SourceGeneratedMethodInformation
    {
        Type = typeof(ILastTestInAssemblyEventReceiver),
        Attributes = [],
        Name = nameof(lastTestInAssemblyEventReceiver.OnLastTestInAssembly),
        Parameters = [new SourceGeneratedParameterInformation<AssemblyHookContext>
        {
            Attributes = [],
            Name = "context"
        }, new SourceGeneratedParameterInformation<TestContext>
        {
            Attributes = [],
            Name = "testContext"
        }],
        GenericTypeCount = 0,
        ReturnType = typeof(ValueTask),
        Class = new SourceGeneratedClassInformation
        {
            Type = typeof(ILastTestInAssemblyEventReceiver),
            Assembly = new SourceGeneratedAssemblyInformation
            {
                Name = "TUnit.Core",
                Attributes = [],
            },
            Attributes = [],
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