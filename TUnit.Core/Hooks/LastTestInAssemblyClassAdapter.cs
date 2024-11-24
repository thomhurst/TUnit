using System.Reflection;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Hooks;

public class LastTestInAssemblyAdapter(ILastTestInAssemblyEventReceiver lastTestInAssemblyEventReceiver, TestContext testContext) : IExecutableHook<AssemblyHookContext>
{
    public string Name => nameof(lastTestInAssemblyEventReceiver.IfLastTestInAssembly);
    public MethodInfo MethodInfo => typeof(ILastTestInAssemblyEventReceiver).GetMethod(Name)!;
    public int Order => 0;

    public bool Execute(AssemblyHookContext context, CancellationToken cancellationToken)
    {
        return false;
    }

    public async Task ExecuteAsync(AssemblyHookContext context, CancellationToken cancellationToken)
    {
        await lastTestInAssemblyEventReceiver.IfLastTestInAssembly(context, testContext);
    }

    public bool IsSynchronous => false;
}