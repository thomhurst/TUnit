using System.Runtime.InteropServices;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class AssemblyLoaderTests : TestsBase<AssemblyLoaderGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BasicTests.cs"),
        new RunTestOptions()
        {
            VerifyConfigurator = verify =>
            {
                return verify.UniqueForTargetFrameworkAndVersion()
                    .ScrubLinesWithReplace(line =>
                    {
                        if (line.Contains("public static class AssemblyLoader"))
                        {
                            return "public static class AssemblyLoader_Guid";
                        }

                        return line;
                    });
            }
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
        });
}