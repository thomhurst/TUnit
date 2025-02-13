using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeQuality.Analyzers.QualityGuidelines;
using NUnit.Framework;

namespace TUnit.Analyzers.Tests;

public class MarkMethodStaticSuppressorTests
{
    private static readonly DiagnosticResult CA1822 = new("CA1822", DiagnosticSeverity.Info);
    
    [TestCase("Test")]
    [TestCase("Before(Test)")]
    [TestCase("After(Test)")]
    public async Task WarningsInTUnitAreSuppressed(string attribute) =>
        await AnalyzerTestHelpers
            .CreateSuppressorTest<MarkMethodStaticSuppressor>(
                $$"""
                using TUnit.Core;
                using static TUnit.Core.HookType;
                
                public class MyTests
                {
                    [{{attribute}}]
                    public void {|#0:Test1|}() 
                    {
                    }
                }
                """
            )
            .WithAnalyzer<MarkMembersAsStaticAnalyzer>()
            .WithSpecificDiagnostics(CA1822)
            .WithExpectedDiagnosticsResults(CA1822.WithLocation(0).WithIsSuppressed(true))
            .RunAsync();

    [Test]
    public async Task WarningsAllowedElsewhere() =>
        await AnalyzerTestHelpers
            .CreateSuppressorTest<MarkMethodStaticSuppressor>(
                """
                public class MyTests
                {
                    public void {|#0:DoSomething|}() 
                    {
                    }
                }
                """
            )
            .WithAnalyzer<MarkMembersAsStaticAnalyzer>()
            .WithSpecificDiagnostics(CA1822)
            .WithExpectedDiagnosticsResults(CA1822.WithLocation(0).WithIsSuppressed(false))
            .RunAsync();
}