using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.MissingPolyfillAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class MissingPolyfillAnalyzerTests
{
    [Test]
    public async Task No_Error_On_Modern_Tfm()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                public class MyClass
                {
                }
                """,
                test =>
                {
                    test.TestState.AdditionalReferences.Clear();
                }
            );
    }

    [Test]
    public async Task Error_When_ModuleInitializerAttribute_Missing()
    {
        var net48References = new ReferenceAssemblies(
            "net48",
            new PackageIdentity("Microsoft.NETFramework.ReferenceAssemblies.net48", "1.0.3"),
            System.IO.Path.Combine("ref", "net48"));

        await Verifier
            .VerifyAnalyzerAsync(
                """
                public class MyClass
                {
                }
                """,
                test =>
                {
                    test.ReferenceAssemblies = net48References;
                    test.TestState.AdditionalReferences.Clear();
                    test.CompilerDiagnostics = CompilerDiagnostics.None;
                },
                Verifier
                    .Diagnostic(Rules.MissingPolyfillPackage)
                    .WithArguments("System.Runtime.CompilerServices.ModuleInitializerAttribute")
            );
    }

    [Test]
    public async Task No_Error_When_ModuleInitializerAttribute_Provided_By_Polyfill()
    {
        var net48References = new ReferenceAssemblies(
            "net48",
            new PackageIdentity("Microsoft.NETFramework.ReferenceAssemblies.net48", "1.0.3"),
            System.IO.Path.Combine("ref", "net48"));

        await Verifier
            .VerifyAnalyzerAsync(
                """
                namespace System.Runtime.CompilerServices
                {
                    internal sealed class ModuleInitializerAttribute : Attribute
                    {
                    }
                }

                public class MyClass
                {
                }
                """,
                test =>
                {
                    test.ReferenceAssemblies = net48References;
                    test.TestState.AdditionalReferences.Clear();
                    test.CompilerDiagnostics = CompilerDiagnostics.None;
                }
            );
    }

    [Test]
    public async Task No_Error_When_ModuleInitializerAttribute_In_Multiple_Referenced_Assemblies()
    {
        // On modern TFMs, ModuleInitializerAttribute is in the runtime.
        // When a referenced library also embeds it via Polyfill,
        // GetTypeByMetadataName returns null due to ambiguity.
        // The analyzer should not report TUnit0073 in this case.
        var refs = await ReferenceAssemblies.Net.Net90.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);

        var attributeSource = CSharpSyntaxTree.ParseText("""
            namespace System.Runtime.CompilerServices
            {
                internal sealed class ModuleInitializerAttribute : Attribute
                {
                }
            }
            """);

        using var stream = new MemoryStream();
        var result = CSharpCompilation.Create("PolyfillLibrary",
                [attributeSource],
                refs,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .Emit(stream);
        await TUnit.Assertions.Assert.That(result.Success).IsTrue();

        await Verifier
            .VerifyAnalyzerAsync(
                """
                public class MyClass
                {
                }
                """,
                test =>
                {
                    test.TestState.AdditionalReferences.Clear();
                    test.TestState.AdditionalReferences.Add(
                        MetadataReference.CreateFromImage(stream.ToArray()));
                    test.CompilerDiagnostics = CompilerDiagnostics.None;
                }
            );
    }
}
