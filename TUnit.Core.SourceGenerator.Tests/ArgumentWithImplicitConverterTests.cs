
namespace TUnit.Core.SourceGenerator.Tests;

internal class ArgumentWithImplicitConverterTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ArgumentWithImplicitConverterTests.cs"),
        async generatedFiles =>
        {
            });
}
