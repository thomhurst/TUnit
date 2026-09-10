
namespace TUnit.Core.SourceGenerator.Tests;

internal class TupleDataSourceDrivenTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "TupleDataSourceDrivenTests.cs"),
        async generatedFiles =>
        {
            });
}
