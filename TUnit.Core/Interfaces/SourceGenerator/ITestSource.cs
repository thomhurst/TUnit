namespace TUnit.Core.Interfaces.SourceGenerator;

public interface ITestSource
{
    Task<IReadOnlyList<TestConstructionData>> CollectTestsAsync(string sessionId);
}