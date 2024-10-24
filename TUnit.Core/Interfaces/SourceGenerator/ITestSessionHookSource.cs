namespace TUnit.Core.Interfaces.SourceGenerator;

public interface ITestSessionHookSource
{
    IReadOnlyList<StaticHookMethod<TestSessionContext>> CollectBeforeHooks();
    IReadOnlyList<StaticHookMethod<TestSessionContext>> CollectAfterHooks();
}