
namespace TUnit.Core.SourceGenerator.Tests;

internal class AssemblyBeforeTests : TestsBase
{
    [Test]
    public Task Test() => HooksGenerator.RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "BeforeTests",
            "AssemblyBeforeTests.cs"),
        async generatedFiles =>
        {
            });
}
