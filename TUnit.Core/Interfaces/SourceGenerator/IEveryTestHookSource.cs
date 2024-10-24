namespace TUnit.Core.Interfaces.SourceGenerator;

public interface IEveryTestHookSource
{
    IReadOnlyList<StaticHookMethod<TestContext>> CollectBeforeHooks();
    IReadOnlyList<StaticHookMethod<TestContext>> CollectAfterHooks();
}