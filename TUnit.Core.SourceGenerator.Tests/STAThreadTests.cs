using TUnit.Core.Enums;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class STAThreadTests : TestsBase<TestsGenerator>
{
    [Test, RunOn(OS.Windows)]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "STAThreadTests.cs"),
        new RunTestOptions
        {
            VerifyConfigurator = verify => verify.UniqueForTargetFrameworkAndVersion()
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(13);
        });
}
