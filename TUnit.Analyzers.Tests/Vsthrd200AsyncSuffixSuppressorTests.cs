#if NET8_0_OR_GREATER
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.Threading.Analyzers;

namespace TUnit.Analyzers.Tests;

public class Vsthrd200AsyncSuffixSuppressorTests
{
    private static readonly DiagnosticResult VSTHRD200 = new("VSTHRD200", DiagnosticSeverity.Warning);

    [Test]
    [Arguments("Test")]
    [Arguments("Before(Test)")]
    [Arguments("After(Test)")]
    [Arguments("BeforeEvery(Test)")]
    [Arguments("AfterEvery(Test)")]
    public async Task WarningsOnTestAndHookMethodsAreSuppressed(string attribute) =>
        await AnalyzerTestHelpers
            .CreateSuppressorTest<Vsthrd200AsyncSuffixSuppressor>(
                $$"""
                using System.Threading.Tasks;
                using TUnit.Core;
                using static TUnit.Core.HookType;

                public class MyTests
                {
                    [{{attribute}}]
                    public async Task {|#0:Foo|}()
                    {
                        await Task.CompletedTask;
                    }
                }
                """
            )
            .WithAnalyzer<VSTHRD200UseAsyncNamingConventionAnalyzer>()
            .WithSpecificDiagnostics(VSTHRD200)
            .WithExpectedDiagnosticsResults(VSTHRD200.WithLocation(0).WithIsSuppressed(true))
            .RunAsync();

    [Test]
    public async Task WarningsAllowedElsewhere() =>
        await AnalyzerTestHelpers
            .CreateSuppressorTest<Vsthrd200AsyncSuffixSuppressor>(
                """
                using System.Threading.Tasks;

                public class MyTests
                {
                    public async Task {|#0:DoSomething|}()
                    {
                        await Task.CompletedTask;
                    }
                }
                """
            )
            .WithAnalyzer<VSTHRD200UseAsyncNamingConventionAnalyzer>()
            .WithSpecificDiagnostics(VSTHRD200)
            .WithExpectedDiagnosticsResults(VSTHRD200.WithLocation(0).WithIsSuppressed(false))
            .RunAsync();
}
#endif
