
namespace TUnit.Core.SourceGenerator.Tests;

internal class ClassAndMethodArgumentsTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassAndMethodArgumentsTests.cs"),
        async generatedFiles =>
        {
            });
}
