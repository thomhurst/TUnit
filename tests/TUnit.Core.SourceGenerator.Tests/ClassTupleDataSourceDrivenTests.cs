
namespace TUnit.Core.SourceGenerator.Tests;

internal class ClassTupleDataSourceDrivenTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "ClassTupleDataSourceDrivenTests.cs"),
        async generatedFiles =>
        {
            });
}
