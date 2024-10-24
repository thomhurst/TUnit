namespace TUnit.Core.Interfaces.SourceGenerator;

public interface ITestSessionHookSource
{
    IReadOnlyList<StaticHookMethod<TestSessionContext>> CollectBeforeTestSessionHooks();
    IReadOnlyList<StaticHookMethod<TestSessionContext>> CollectAfterTestSessionHooks();
}