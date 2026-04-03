
namespace TUnit.Core.SourceGenerator.Tests;

internal class NullableParsableArgumentTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "StringToParsableArgumentsTests.cs"),
        async generatedFiles =>
        {
            });
}
