namespace TUnit.Core.Interfaces.SourceGenerator;

public interface ITestHookSource
{
    IReadOnlyList<InstanceHookMethod> CollectBeforeTestHooks();
    IReadOnlyList<InstanceHookMethod> CollectAfterTestHooks();
    
    IReadOnlyList<StaticHookMethod<TestContext>> CollectBeforeEveryTestHooks();
    IReadOnlyList<StaticHookMethod<TestContext>> CollectAfterEveryTestHooks();
}