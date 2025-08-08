namespace TUnit.Core.Interfaces.SourceGenerator;

public interface ITestSource
{
    IAsyncEnumerable<TestMetadata> GetTestsAsync(string testSessionId, CancellationToken cancellationToken = default);
}
