
namespace TUnit.Core.SourceGenerator.Tests;

internal class AsyncMethodDataSourceDrivenTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
        "TUnit.TestProject",
        "AsyncMethodDataSourceDrivenTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsGreaterThan(0);
        });
}
