using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace TUnit.Assertions.Analyzers.Tests.Verifiers;

public static partial class CSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    private static ReferenceAssemblies GetReferenceAssemblies()
    {
#if NET472
        return ReferenceAssemblies.NetFramework.Net472.Default;
#elif NET8_0
        return ReferenceAssemblies.Net.Net80;
#elif NET9_0 || NET10_0_OR_GREATER
        return ReferenceAssemblies.Net.Net90;
#else
        return ReferenceAssemblies.Net.Net80; // Default fallback
#endif
    }
    private static string GetCompatibleShouldDllPath()
    {
        // The netstandard2.0 build of TUnit.Assertions.Should is copied into the test bin as
        // "TUnit.Assertions.Should.netstandard2.0.dll" by the csproj. It targets netstandard2.0
        // so loading it doesn't pull in System.Runtime v10, which would otherwise CS1705
        // against the analyzer-test framework's net9.0 reference assemblies.
        var ns20Path = Path.Combine(AppContext.BaseDirectory, "TUnit.Assertions.Should.netstandard2.0.dll");
        return File.Exists(ns20Path) ? ns20Path : typeof(TUnit.Assertions.Should.ShouldExtensions).Assembly.Location;
    }

    /// <inheritdoc cref="Microsoft.CodeAnalysis.Diagnostic"/>
    public static DiagnosticResult Diagnostic()
        => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic();

    /// <inheritdoc cref="Microsoft.CodeAnalysis.Diagnostic"/>
    public static DiagnosticResult Diagnostic(string diagnosticId)
        => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);

    /// <inheritdoc cref="Microsoft.CodeAnalysis.Diagnostic"/>
    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(descriptor);

    /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
    public static async Task VerifyAnalyzerAsync([StringSyntax("c#-test")] string source, params DiagnosticResult[] expected)
    {
        var test = new Test
        {
            TestCode = source,
            ReferenceAssemblies = GetReferenceAssemblies()
                .AddPackages([new PackageIdentity("xunit.v3.assert", "2.0.0")]),
            TestState =
            {
                AdditionalReferences =
                {
                    typeof(TUnitAttribute).Assembly.Location,
                    typeof(Assert).Assembly.Location,
                    // The analyzer test framework references net9.0 BCL — load the netstandard2.0
                    // build of TUnit.Assertions.Should so System.Runtime version mismatches
                    // (CS1705) don't surface, while keeping the same public API surface.
                    GetCompatibleShouldDllPath(),
                },
            },
            CompilerDiagnostics = CompilerDiagnostics.None
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }
}
