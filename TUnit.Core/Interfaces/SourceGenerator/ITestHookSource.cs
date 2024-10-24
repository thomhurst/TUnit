namespace TUnit.Core.Interfaces.SourceGenerator;

public interface ITestHookSource
{
    IReadOnlyList<InstanceHookMethod> CollectBeforeHooks();
    IReadOnlyList<InstanceHookMethod> CollectAfterHooks();
}