using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Polly.CircuitBreaker;
using TUnit.TestProject.Library;

namespace TUnit.Analyzers.Tests;

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
                    MetadataReference.CreateFromFile(typeof(TUnitAttribute).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(CircuitState).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(ProjectReferenceEnum).Assembly.Location)
                ]
            );

        return csTest;
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
        var currentVersion = Environment.Version.Major;
        var referenceAssemblies = GetReferenceAssembliesForCurrentVersion(currentVersion);

        var test = new CSharpSuppressorTest<TSuppressor, DefaultVerifier>
        {
            TestCode = inputSource,
            ReferenceAssemblies = referenceAssemblies
        };

        test.TestState.AdditionalReferences
            .AddRange([
                MetadataReference.CreateFromFile(typeof(TUnitAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(CircuitState).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ProjectReferenceEnum).Assembly.Location)
            ]);

        return test;
    }

    private static ReferenceAssemblies GetReferenceAssembliesForCurrentVersion(int currentVersion)
    {
        if (currentVersion == 4)
        {
            return new ReferenceAssemblies(
                "net48",
                new PackageIdentity("Microsoft.NETFramework.ReferenceAssemblies.net48", "1.0.3"),
                Path.Combine("ref", "net48"));
        }

        return new ReferenceAssemblies(
            $"net{currentVersion}.0",
            new PackageIdentity(
                "Microsoft.NETCore.App.Ref",
                $"{currentVersion}.0.0"),
            Path.Combine("ref", $"net{currentVersion}.0")
        );
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

file static class DiagnosticSeverityExtensions
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
