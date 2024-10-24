namespace TUnit.Core.Interfaces.SourceGenerator;

public interface ITestDiscoveryHookSource
{
    IReadOnlyList<StaticHookMethod<BeforeTestDiscoveryContext>> CollectBeforeHooks();
    IReadOnlyList<StaticHookMethod<TestDiscoveryContext>> CollectAfterHooks();
}