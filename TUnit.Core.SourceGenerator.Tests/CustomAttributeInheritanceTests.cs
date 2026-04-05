using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class CustomAttributeInheritanceTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "CustomAttributeInheritanceTests.cs"),
        new RunTestOptions(),
        async generatedFiles =>
        {
        });
}
