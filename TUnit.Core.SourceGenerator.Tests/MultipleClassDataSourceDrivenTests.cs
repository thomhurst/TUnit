
namespace TUnit.Core.SourceGenerator.Tests;

internal class MultipleClassDataSourceDrivenTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "MultipleClassDataSourceDrivenTests.cs"),
        async generatedFiles =>
        {
            });
}
