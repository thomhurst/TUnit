namespace TUnit.Core.Interfaces.SourceGenerator;

public interface ITestSource
{
    IReadOnlyList<TestMetadata> CollectTests(string sessionId);
}