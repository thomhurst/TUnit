using System.Reflection;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Hooks;

public class LastTestInClassAdapter(ILastTestInClassEventReceiver lastTestInClassEventReceiver, TestContext testContext) : IExecutableHook<ClassHookContext>
{
    public string Name => nameof(lastTestInClassEventReceiver.OnLastTestInClass);
    public MethodInfo MethodInfo => typeof(ILastTestInClassEventReceiver).GetMethod(Name)!;
    public int Order => 0;

    public bool Execute(ClassHookContext context, CancellationToken cancellationToken)
    {
        return false;
    }

    public async Task ExecuteAsync(ClassHookContext context, CancellationToken cancellationToken)
    {
        await lastTestInClassEventReceiver.OnLastTestInClass(context, testContext);
    }

    public bool IsSynchronous => false;
}