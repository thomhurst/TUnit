namespace TUnit.Core.Interfaces.SourceGenerator;

public interface ITestHookSource
{
    IReadOnlyList<InstanceHookMethod> CollectBeforeHooks();
    IReadOnlyList<InstanceHookMethod> CollectAfterHooks();
    
    IReadOnlyList<StaticHookMethod<TestContext>> CollectBeforeEveryHooks();
    IReadOnlyList<StaticHookMethod<TestContext>> CollectAfterEveryHooks();
}