using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class BasicTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BasicTests.cs"),
        new RunTestOptions
        {
            VerifyConfigurator = verify => verify.UniqueForTargetFrameworkAndVersion()
        },
        async generatedFiles =>
        {
            });
}
