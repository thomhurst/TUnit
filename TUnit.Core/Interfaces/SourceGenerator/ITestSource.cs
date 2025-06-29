namespace TUnit.Core.Interfaces.SourceGenerator;

public interface ITestSource
{
    Task<IReadOnlyList<TestMetadata>> CollectTestsAsync(string sessionId);
}