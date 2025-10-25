using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using TUnit.Core;

namespace TUnit.Assertions.Analyzers.Tests;

/// <summary>
/// Tests for the IsNotNullAssertionSuppressor which suppresses nullability warnings
/// (CS8600, CS8602, CS8604, CS8618) for variables after Assert.That(x).IsNotNull().
///
/// Note: These tests verify that the suppressor correctly identifies and suppresses
/// nullability warnings. The suppressor does not change null-state flow analysis,
/// only suppresses the resulting warnings.
/// </summary>
public class IsNotNullAssertionSuppressorTests
{
    private static readonly DiagnosticResult CS8602 = new("CS8602", DiagnosticSeverity.Warning);
    private static readonly DiagnosticResult CS8604 = new("CS8604", DiagnosticSeverity.Warning);
    private static readonly DiagnosticResult CS1591 = new("CS1591", DiagnosticSeverity.Warning);

    [Test]
    public async Task Suppresses_CS8602_After_IsNotNull_Assertion()
    {
        const string code = """
            #nullable enable
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Extensions;

            public class MyTests
            {
                public async Task TestMethod()
                {
                    string? nullableString = GetNullableString();

                    await Assert.That(nullableString).IsNotNull();

                    // This would normally produce CS8602: Dereference of a possibly null reference
                    // But the suppressor should suppress it after IsNotNull assertion
                    var length = {|#0:nullableString|}.Length;
                }

                private string? GetNullableString() => "test";
            }
            """;

        await AnalyzerTestHelpers
            .CreateSuppressorTest<IsNotNullAssertionSuppressor>(code)
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8602)
            .WithExpectedDiagnosticsResults(CS8602.WithLocation(0).WithIsSuppressed(true))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    public async Task Suppresses_CS8604_After_IsNotNull_Assertion()
    {
        const string code = """
            #nullable enable
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Extensions;

            public class MyTests
            {
                public async Task TestMethod()
                {
                    string? nullableString = GetNullableString();

                    await Assert.That(nullableString).IsNotNull();

                    // This would normally produce CS8604: Possible null reference argument
                    // But the suppressor should suppress it after IsNotNull assertion
                    AcceptsNonNull({|#0:nullableString|});
                }

                private void AcceptsNonNull(string nonNull) { }
                private string? GetNullableString() => "test";
            }
            """;

        await AnalyzerTestHelpers
            .CreateSuppressorTest<IsNotNullAssertionSuppressor>(code)
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8604)
            .WithExpectedDiagnosticsResults(CS8604.WithLocation(0).WithIsSuppressed(true))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    public async Task Does_Not_Suppress_Without_IsNotNull_Assertion()
    {
        const string code = """
            #nullable enable
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Extensions;

            public class MyTests
            {
                public void TestMethod()
                {
                    string? nullableString = GetNullableString();

                    // No IsNotNull assertion here

                    // This should still produce CS8602 warning
                    var length = {|#0:nullableString|}.Length;
                }

                private string? GetNullableString() => "test";
            }
            """;

        await AnalyzerTestHelpers
            .CreateSuppressorTest<IsNotNullAssertionSuppressor>(code)
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8602)
            .WithExpectedDiagnosticsResults(CS8602.WithLocation(0).WithIsSuppressed(false))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    public async Task Suppresses_Multiple_Uses_After_IsNotNull()
    {
        const string code = """
            #nullable enable
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Extensions;

            public class MyTests
            {
                public async Task TestMethod()
                {
                    string? nullableString = GetNullableString();

                    await Assert.That(nullableString).IsNotNull();

                    // Multiple uses should all be suppressed
                    var length = {|#0:nullableString|}.Length;
                    var upper = {|#1:nullableString|}.ToUpper();
                    AcceptsNonNull({|#2:nullableString|});
                }

                private void AcceptsNonNull(string nonNull) { }
                private string? GetNullableString() => "test";
            }
            """;

        await AnalyzerTestHelpers
            .CreateSuppressorTest<IsNotNullAssertionSuppressor>(code)
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8602)
            .WithExpectedDiagnosticsResults(
                CS8602.WithLocation(0).WithIsSuppressed(true),
                CS8602.WithLocation(1).WithIsSuppressed(true),
                CS8604.WithLocation(2).WithIsSuppressed(true)
            )
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    public async Task Suppresses_Only_Asserted_Variable()
    {
        const string code = """
            #nullable enable
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Extensions;

            public class MyTests
            {
                public async Task TestMethod()
                {
                    string? nullableString1 = GetNullableString();
                    string? nullableString2 = GetNullableString();

                    await Assert.That(nullableString1).IsNotNull();

                    // nullableString1 should be suppressed
                    var length1 = {|#0:nullableString1|}.Length;

                    // nullableString2 should NOT be suppressed (not asserted)
                    var length2 = {|#1:nullableString2|}.Length;
                }

                private string? GetNullableString() => "test";
            }
            """;

        await AnalyzerTestHelpers
            .CreateSuppressorTest<IsNotNullAssertionSuppressor>(code)
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8602)
            .WithExpectedDiagnosticsResults(
                CS8602.WithLocation(0).WithIsSuppressed(true),
                CS8602.WithLocation(1).WithIsSuppressed(false)
            )
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    public async Task Suppresses_Property_Access_Chain()
    {
        const string code = """
            #nullable enable
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Extensions;

            public class MyClass
            {
                public string? Property { get; set; }
            }

            public class MyTests
            {
                public async Task TestMethod()
                {
                    MyClass? obj = GetNullableObject();

                    await Assert.That(obj).IsNotNull();

                    // This should be suppressed
                    var prop = {|#0:obj|}.Property;
                }

                private MyClass? GetNullableObject() => new MyClass();
            }
            """;

        await AnalyzerTestHelpers
            .CreateSuppressorTest<IsNotNullAssertionSuppressor>(code)
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8602)
            .WithExpectedDiagnosticsResults(CS8602.WithLocation(0).WithIsSuppressed(true))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    public async Task Suppresses_After_IsNotNull_At_Start_Of_Assertion_Chain()
    {
        const string code = """
            #nullable enable
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Extensions;

            public class MyTests
            {
                public async Task TestMethod()
                {
                    string? nullableString = GetNullableString();

                    // IsNotNull at the START of the chain
                    await Assert.That(nullableString).IsNotNull().And.Contains("test");

                    // After the assertion chain, should be suppressed
                    var length = {|#0:nullableString|}.Length;
                }

                private string? GetNullableString() => "test";
            }
            """;

        await AnalyzerTestHelpers
            .CreateSuppressorTest<IsNotNullAssertionSuppressor>(code)
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8602)
            .WithExpectedDiagnosticsResults(CS8602.WithLocation(0).WithIsSuppressed(true))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    public async Task Suppresses_After_IsNotNull_At_End_Of_Assertion_Chain()
    {
        const string code = """
            #nullable enable
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Extensions;

            public class MyTests
            {
                public async Task TestMethod()
                {
                    string? nullableString = GetNullableString();

                    // IsNotNull at the END of the chain
                    await Assert.That(nullableString).Contains("test").And.IsNotNull();

                    // After the assertion chain, should be suppressed
                    var length = {|#0:nullableString|}.Length;
                }

                private string? GetNullableString() => "test";
            }
            """;

        await AnalyzerTestHelpers
            .CreateSuppressorTest<IsNotNullAssertionSuppressor>(code)
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8602)
            .WithExpectedDiagnosticsResults(CS8602.WithLocation(0).WithIsSuppressed(true))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    public async Task Suppresses_After_IsNotNull_In_Middle_Of_Assertion_Chain()
    {
        const string code = """
            #nullable enable
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Extensions;

            public class MyTests
            {
                public async Task TestMethod()
                {
                    string? nullableString = GetNullableString();

                    // IsNotNull in the MIDDLE of the chain
                    await Assert.That(nullableString).Contains("t").And.IsNotNull().And.Contains("test");

                    // After the assertion chain, should be suppressed
                    var length = {|#0:nullableString|}.Length;
                }

                private string? GetNullableString() => "test";
            }
            """;

        await AnalyzerTestHelpers
            .CreateSuppressorTest<IsNotNullAssertionSuppressor>(code)
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8602)
            .WithExpectedDiagnosticsResults(CS8602.WithLocation(0).WithIsSuppressed(true))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    public async Task Suppresses_After_IsNotNull_With_Or_Chain()
    {
        const string code = """
            #nullable enable
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Extensions;

            public class MyTests
            {
                public async Task TestMethod()
                {
                    string? nullableString = GetNullableString();

                    // IsNotNull with Or chain
                    await Assert.That(nullableString).IsNotNull().Or.IsEqualTo("fallback");

                    // After the assertion, should be suppressed
                    var length = {|#0:nullableString|}.Length;
                }

                private string? GetNullableString() => "test";
            }
            """;

        await AnalyzerTestHelpers
            .CreateSuppressorTest<IsNotNullAssertionSuppressor>(code)
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8602)
            .WithExpectedDiagnosticsResults(CS8602.WithLocation(0).WithIsSuppressed(true))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    public async Task Suppresses_Multiple_Variables_With_Chained_Assertions()
    {
        const string code = """
            #nullable enable
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Extensions;

            public class MyTests
            {
                public async Task TestMethod()
                {
                    string? str1 = GetNullableString();
                    string? str2 = GetNullableString();

                    // Both variables asserted
                    await Assert.That(str1).IsNotNull().And.Contains("test");
                    await Assert.That(str2).IsNotNull();

                    // Both should be suppressed
                    var length1 = {|#0:str1|}.Length;
                    var length2 = {|#1:str2|}.Length;
                }

                private string? GetNullableString() => "test";
            }
            """;

        await AnalyzerTestHelpers
            .CreateSuppressorTest<IsNotNullAssertionSuppressor>(code)
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8602)
            .WithExpectedDiagnosticsResults(
                CS8602.WithLocation(0).WithIsSuppressed(true),
                CS8602.WithLocation(1).WithIsSuppressed(true)
            )
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }
}
