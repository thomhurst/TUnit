namespace TUnit.Core.Interfaces.SourceGenerator;

public interface ITestSessionHookSource
{
    IReadOnlyList<StaticHookMethod<TestSessionContext>> CollectBeforeTestSessionHooks(string sessionId);
    IReadOnlyList<StaticHookMethod<TestSessionContext>> CollectAfterTestSessionHooks(string sessionId);
}