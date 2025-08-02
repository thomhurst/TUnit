namespace TUnit.Core.Interfaces.SourceGenerator;

public interface ITestSource
{
    ValueTask<List<TestMetadata>> GetTestsAsync(string testSessionId);
}
