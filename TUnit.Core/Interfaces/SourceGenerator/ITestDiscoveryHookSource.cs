namespace TUnit.Core.Interfaces.SourceGenerator;

public interface ITestDiscoveryHookSource
{
    IReadOnlyList<StaticHookMethod<BeforeTestDiscoveryContext>> CollectBeforeTestDiscoveryHooks();
    IReadOnlyList<StaticHookMethod<TestDiscoveryContext>> CollectAfterTestDiscoveryHooks();
}