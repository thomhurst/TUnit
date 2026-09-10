
namespace TUnit.Core.SourceGenerator.Tests;

internal class NullableByteArgumentTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "NullableByteArgumentTests.cs"),
        async generatedFiles =>
        {
            });
}
