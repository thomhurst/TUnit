
namespace TUnit.Core.SourceGenerator.Tests;

internal class ClassAndMethodArgumentsTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "ClassAndMethodArgumentsTests.cs"),
        async generatedFiles =>
        {
            });
}
