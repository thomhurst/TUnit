
namespace TUnit.Core.SourceGenerator.Tests;

internal class MethodDataSourceDrivenTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "MethodDataSourceDrivenTests.cs"),
        async generatedFiles =>
        {
            });
}
