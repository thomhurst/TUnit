namespace TUnit.Core.Interfaces.SourceGenerator;

public interface ITestSource
{
    IReadOnlyList<SourceGeneratedTestNode> CollectTests(string sessionId);
}