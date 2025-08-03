
namespace TUnit.Core.SourceGenerator.Tests;

internal class AssemblyAfterTests : TestsBase
{
    [Test]
    public Task Test() => HooksGenerator.RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AfterTests",
            "AssemblyAfterTests.cs"),
        async generatedFiles =>
        {
            });
}
