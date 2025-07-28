
namespace TUnit.Core.SourceGenerator.Tests;

internal class ClassDataSourceDrivenTests2 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassDataSourceDrivenTests2.cs"),
        async generatedFiles =>
        {
            });
}
