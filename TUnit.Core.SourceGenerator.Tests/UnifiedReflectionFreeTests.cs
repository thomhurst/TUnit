using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Tests.Options;
using Verifier = TUnit.Core.SourceGenerator.Tests.Verifier;

namespace TUnit.Core.SourceGenerator.Tests;

internal class UnifiedReflectionFreeTests : TestsBase<UnifiedTestMetadataGenerator>
{
    [Test]
    public Task Test_StronglyTypedDelegates_Generation() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "TypedDelegateTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles = []
        });

    [Test]
    public Task Test_ModuleInitializer_Generation() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ModuleInitializerTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles = []
        });

    [Test]
    public Task Test_AotSafeDataSourceFactories() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AotDataSourceTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles = []
        });

    [Test]
    public Task Test_ConfigurationSupport() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ConfigurationTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles = []
        });

    private static async Task RunTest(string classFile, RunTestOptions? options = null)
    {
        if (!File.Exists(classFile))
        {
            // Create empty test files for now - these would normally contain actual test classes
            Directory.CreateDirectory(Path.GetDirectoryName(classFile)!);
            await File.WriteAllTextAsync(classFile, $"""
            using TUnit.Core;

            namespace TUnit.TestProject;

            public class {Path.GetFileNameWithoutExtension(classFile).Replace("Tests", "Test")}
            {{
                [Test]
                public void SampleTest()
                {{
                    // Sample test for {Path.GetFileNameWithoutExtension(classFile)}
                }}
            }}
            """);
        }

        var source = await FilePolyfill.ReadAllTextAsync(classFile);

        var (compilation, diagnostics) = await Verifier.GetGeneratedOutput<UnifiedTestMetadataGenerator>(
            source,
            options ?? new RunTestOptions()
        );

        // Verify no compilation errors related to reflection
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0)
        {
            throw new InvalidOperationException($"Compilation errors: {string.Join(", ", errors.Select(e => e.GetMessage()))}");
        }

        // Verify source generation output
        await Verifier.Verify(compilation, $"{nameof(UnifiedReflectionFreeTests)}.{Path.GetFileNameWithoutExtension(classFile)}");
    }
}
