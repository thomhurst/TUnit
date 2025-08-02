
namespace TUnit.Core.SourceGenerator.Tests;

internal class EnumerableTupleDataSourceDrivenTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "EnumerableTupleDataSourceDrivenTests.cs"),
        async generatedFiles =>
        {
            });
}
