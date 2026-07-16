
namespace TUnit.Core.SourceGenerator.Tests;

internal class PropertySetterTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "PropertySetterTests.cs"),
        async generatedFiles =>
        {
            // Static
        });
}
