namespace TUnit.Core.Interfaces.SourceGenerator;

public interface ITestDiscoveryHookSource
{
    IReadOnlyList<StaticHookMethod<BeforeTestDiscoveryContext>> CollectBeforeTestDiscoveryHooks(string sessionId);
    IReadOnlyList<StaticHookMethod<TestDiscoveryContext>> CollectAfterTestDiscoveryHooks(string sessionId);
}