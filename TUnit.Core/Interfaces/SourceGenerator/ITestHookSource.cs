namespace TUnit.Core.Interfaces.SourceGenerator;

public interface ITestHookSource
{
    IReadOnlyList<InstanceHookMethod> CollectBeforeTestHooks(string sessionId);
    IReadOnlyList<InstanceHookMethod> CollectAfterTestHooks(string sessionId);
    
    IReadOnlyList<StaticHookMethod<TestContext>> CollectBeforeEveryTestHooks(string sessionId);
    IReadOnlyList<StaticHookMethod<TestContext>> CollectAfterEveryTestHooks(string sessionId);
}