using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class Bugs2971NullableTypeTest : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "2971",
            "NullableTypeTest.cs"),
        new RunTestOptions
        {
            AdditionalFiles = [Path.Combine(Git.RootDirectory.FullName,
                "TUnit.TestProject",
                "Bugs",
                "2971",
                "AssemblyInfo.cs")],
            VerifyConfigurator = verify => verify.UniqueForTargetFrameworkAndVersion()
        },
        async generatedFiles =>
        {
            await Verify(generatedFiles);
        });
}