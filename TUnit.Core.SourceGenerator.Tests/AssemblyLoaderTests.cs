using Microsoft.CodeAnalysis.Testing;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class AssemblyLoaderTests : TestsBase<AssemblyLoaderGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AssemblyLoaderTest.cs"),
        new RunTestOptions
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
            },
            AdditionalPackages = 
            [
                new PackageIdentity("Confluent.SchemaRegistry.Serdes.Protobuf", "2.10.0"),
                new PackageIdentity("WireMock.Net", "1.7.4"),
            ],
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles)
                .HasSingleItem()
                .And
                .DoesNotContain("typeof(global::Google.Protobuf.Reflection.FileDescriptorSet).Assembly")
                .And
                .DoesNotContain("global::Google.Protobuf.WellKnownTypes.Any).Assembly");
        });
}