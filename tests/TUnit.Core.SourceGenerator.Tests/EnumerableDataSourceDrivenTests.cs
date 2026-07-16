
namespace TUnit.Core.SourceGenerator.Tests;

internal class EnumerableDataSourceDrivenTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "EnumerableDataSourceDrivenTests.cs"),
        async generatedFiles =>
        {
            });
}
