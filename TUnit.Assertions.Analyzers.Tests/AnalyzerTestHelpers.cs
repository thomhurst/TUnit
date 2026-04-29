using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace TUnit.Assertions.Analyzers.Tests;

public static class AnalyzerTestHelpers
{
    public static CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> CreateAnalyzerTest<TAnalyzer>(
        [StringSyntax("c#-test")] string inputSource
    )
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var csTest = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                Sources = { inputSource },
                ReferenceAssemblies = new ReferenceAssemblies(
                    "net8.0",
                    new PackageIdentity(
                        "Microsoft.NETCore.App.Ref",
                        "8.0.0"),
                    Path.Combine("ref", "net8.0")),
            },
        };

        csTest.TestState.AdditionalReferences
            .AddRange(
                [
                    MetadataReference.CreateFromFile(GetCompatibleCoreDllPath()),
                    MetadataReference.CreateFromFile(GetCompatibleAssertionsDllPath()),
                    MetadataReference.CreateFromFile(GetCompatibleShouldDllPath()),
                ]
            );

        return csTest;
    }

    /// <summary>
    /// Resolves a TUnit.Assertions.Should.dll path compatible with the analyzer-test framework's
    /// net9.0 reference assemblies. The csproj copies the netstandard2.0 build into the test bin
    /// so it loads without dragging in System.Runtime v10 (CS1705).
    /// </summary>
    public static string GetCompatibleShouldDllPath()
        => GetCompatibleDllPath("TUnit.Assertions.Should", typeof(TUnit.Assertions.Should.ShouldExtensions).Assembly);

    /// <summary>
    /// Resolves a TUnit.Assertions.dll path compatible with the analyzer-test framework's
    /// net9.0 reference assemblies. Without this redirect, the test process's net10.0 build is
    /// loaded via <c>typeof(Assert).Assembly.Location</c> and silently fails symbol resolution
    /// for every extension method ("IsEqualTo", "Throws", etc.) under Net90 ref assemblies.
    /// </summary>
    public static string GetCompatibleAssertionsDllPath()
        => GetCompatibleDllPath("TUnit.Assertions", typeof(Assert).Assembly);

    /// <summary>
    /// Resolves a TUnit.Core.dll path compatible with the analyzer-test framework's net9.0
    /// reference assemblies (same rationale as <see cref="GetCompatibleAssertionsDllPath"/>).
    /// </summary>
    public static string GetCompatibleCoreDllPath()
        => GetCompatibleDllPath("TUnit.Core", typeof(TUnitAttribute).Assembly);

    private static string GetCompatibleDllPath(string assemblyName, System.Reflection.Assembly fallback)
    {
        var ns20Path = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.netstandard2.0.dll");
        return File.Exists(ns20Path) ? ns20Path : fallback.Location;
    }

    public sealed class CSharpSuppressorTest<TSuppressor, TVerifier> : CSharpAnalyzerTest<TSuppressor, TVerifier>
        where TSuppressor : DiagnosticSuppressor, new()
        where TVerifier : IVerifier, new()
    {
        private readonly List<DiagnosticAnalyzer> _analyzers = [];

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
        {
            return base.GetDiagnosticAnalyzers().Concat(_analyzers);
        }

        public CSharpSuppressorTest<TSuppressor, TVerifier> WithAnalyzer<TAnalyzer>(bool enableDiagnostics = false)
            where TAnalyzer : DiagnosticAnalyzer, new()
        {
            var analyzer = new TAnalyzer();
            _analyzers.Add(analyzer);

            if (enableDiagnostics)
            {
                var diagnosticOptions = analyzer.SupportedDiagnostics
                    .ToImmutableDictionary(
                        descriptor => descriptor.Id,
                        descriptor => descriptor.DefaultSeverity.ToReportDiagnostic()
                    );

                SolutionTransforms.Clear();
                SolutionTransforms.Add(EnableDiagnostics(diagnosticOptions));
            }

            return this;
        }

        public CSharpSuppressorTest<TSuppressor, TVerifier> WithSpecificDiagnostics(
            params DiagnosticResult[] diagnostics
        )
        {
            var diagnosticOptions = diagnostics
                .ToImmutableDictionary(
                    descriptor => descriptor.Id,
                    descriptor => descriptor.Severity.ToReportDiagnostic()
                );

            SolutionTransforms.Clear();
            SolutionTransforms.Add(EnableDiagnostics(diagnosticOptions));
            return this;
        }

        private static Func<Solution, ProjectId, Solution> EnableDiagnostics(
            ImmutableDictionary<string, ReportDiagnostic> diagnostics
        ) =>
            (solution, id) =>
            {
                var options = solution.GetProject(id)?.CompilationOptions
                    ?? throw new InvalidOperationException("Compilation options missing.");

                return solution
                    .WithProjectCompilationOptions(
                        id,
                        options
                            .WithSpecificDiagnosticOptions(diagnostics)
                    );
            };

        public CSharpSuppressorTest<TSuppressor, TVerifier> WithExpectedDiagnosticsResults(
            params DiagnosticResult[] diagnostics
        )
        {
            ExpectedDiagnostics.AddRange(diagnostics);
            return this;
        }

        public CSharpSuppressorTest<TSuppressor, TVerifier> WithCompilerDiagnostics(
            CompilerDiagnostics diagnostics
        )
        {
            CompilerDiagnostics = diagnostics;
            return this;
        }

        public CSharpSuppressorTest<TSuppressor, TVerifier> IgnoringDiagnostics(params string[] diagnostics)
        {
            DisabledDiagnostics.AddRange(diagnostics);
            return this;
        }
    }

    public static CSharpSuppressorTest<TSuppressor, DefaultVerifier> CreateSuppressorTest<TSuppressor>(
        [StringSyntax("c#-test")] string inputSource
    )
        where TSuppressor : DiagnosticSuppressor, new()
    {
        var test = new CSharpSuppressorTest<TSuppressor, DefaultVerifier>
        {
            TestCode = inputSource,
            ReferenceAssemblies = GetReferenceAssemblies()
        };

        test.TestState.AdditionalReferences
            .AddRange([
                MetadataReference.CreateFromFile(GetCompatibleCoreDllPath()),
                MetadataReference.CreateFromFile(GetCompatibleAssertionsDllPath()),
                MetadataReference.CreateFromFile(GetCompatibleShouldDllPath()),
            ]);

        return test;
    }

    private static ReferenceAssemblies GetReferenceAssemblies()
    {
#if NET472
        return ReferenceAssemblies.NetFramework.Net472.Default;
#elif NET8_0
        return ReferenceAssemblies.Net.Net80;
#elif NET9_0_OR_GREATER
        return ReferenceAssemblies.Net.Net90;
#else
        return ReferenceAssemblies.Net.Net80; // Default fallback
#endif
    }

    public static CSharpSuppressorTest<TSuppressor, DefaultVerifier> CreateSuppressorTest<TSuppressor, TAnalyzer>(
        [StringSyntax("c#-test")] string inputSource
    )
        where TSuppressor : DiagnosticSuppressor, new()
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        return CreateSuppressorTest<TSuppressor>(inputSource)
            .WithAnalyzer<TAnalyzer>(enableDiagnostics: true);
    }
}

static file class DiagnosticSeverityExtensions
{
    public static ReportDiagnostic ToReportDiagnostic(this DiagnosticSeverity severity)
        => severity switch
        {
            DiagnosticSeverity.Hidden => ReportDiagnostic.Hidden,
            DiagnosticSeverity.Info => ReportDiagnostic.Info,
            DiagnosticSeverity.Warning => ReportDiagnostic.Warn,
            DiagnosticSeverity.Error => ReportDiagnostic.Error,
            _ => throw new InvalidEnumArgumentException(nameof(severity), (int) severity, typeof(DiagnosticSeverity)),
        };
}
