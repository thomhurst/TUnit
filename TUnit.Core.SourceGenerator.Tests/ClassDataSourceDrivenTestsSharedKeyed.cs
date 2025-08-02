
namespace TUnit.Core.SourceGenerator.Tests;

internal class ClassDataSourceDrivenTestsSharedKeyed : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassDataSourceDrivenTestsSharedKeyed.cs"),
        async generatedFiles =>
        {
            });
}
