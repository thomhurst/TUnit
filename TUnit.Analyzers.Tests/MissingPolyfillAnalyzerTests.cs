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
}
