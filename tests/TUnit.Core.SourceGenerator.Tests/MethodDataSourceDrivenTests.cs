
namespace TUnit.Core.SourceGenerator.Tests;

internal class MethodDataSourceDrivenTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "MethodDataSourceDrivenTests.cs"),
        async generatedFiles =>
        {
            });
}
