
namespace TUnit.Core.SourceGenerator.Tests;

internal class ConstantArgumentsTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "ConstantArgumentsTests.cs"),
        async generatedFiles =>
        {
            });
}
