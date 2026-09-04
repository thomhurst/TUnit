using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using TUnit.Core;

namespace TUnit.Assertions.Analyzers.Tests;

/// <summary>
/// Tests for the IsNotNullAssertionSuppressor which suppresses nullability warnings
/// (CS8600, CS8602, CS8604, CS8618, CS8629) for variables after Assert.That(x).IsNotNull().
///
/// Note: These tests verify that the suppressor correctly identifies and suppresses
/// nullability warnings. The suppressor does not change null-state flow analysis,
/// only suppresses the resulting warnings.
/// </summary>
public class IsNotNullAssertionSuppressorTests
{
    private static readonly DiagnosticResult CS8600 = new("CS8600", DiagnosticSeverity.Warning);
    private static readonly DiagnosticResult CS8602 = new("CS8602", DiagnosticSeverity.Warning);
    private static readonly DiagnosticResult CS8604 = new("CS8604", DiagnosticSeverity.Warning);
    private static readonly DiagnosticResult CS8629 = new("CS8629", DiagnosticSeverity.Warning);

    [Test]
    [Arguments("int[]")]
    [Arguments("IList<int>")]
    [Arguments("IReadOnlyList<int>")]
    [Arguments("IEnumerable<int>")]
    [Arguments("ICollection<int>")]
    [Arguments("IReadOnlyCollection<int>")]
    [Arguments("IDictionary<string, int>")]
    [Arguments("IReadOnlyDictionary<string, int>")]
    [Arguments("ISet<int>")]
    public async Task Suppresses_After_Collection_IsNotNull_Instance_Method(string collectionType)
    {
        var code = $$"""
            #nullable enable
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Extensions;

            public class MyTests
            {
                public async Task TestMethod({{collectionType}}? values)
                {
                    await Assert.That(values).IsNotNull();
                    Consume({|#0:values|});
                }

                public async Task DereferenceTest({{collectionType}}? values)
                {
                    await Assert.That(values).IsNotNull();
                    _ = {|#1:values|}.GetHashCode();
                }

                public {{collectionType}}? Values { get; set; }

                public async Task PropertyTest(MyTests model)
                {
                    await Assert.That(model.Values).IsNotNull();
                    Consume(model.Values!);
                    Consume({|#2:model.Values|});
                }

                public async Task ForeachTest({{collectionType}}? values)
                {
                    await Assert.That(values).IsNotNull();
                    Consume(values!);
                    foreach (var item in {|#3:values|}) { }
                }

                public async Task ChainStartTest({{collectionType}}? values)
                {
                    await Assert.That(values).IsNotNull().And.IsEmpty();
                    Consume({|#4:values|});
                }

                public async Task ChainEndTest({{collectionType}}? values)
                {
                    await Assert.That(values).IsEmpty().And.IsNotNull();
                    Consume({|#5:values|});
                }

                public async Task AssignmentTest({{collectionType}}? values)
                {
                    await Assert.That(values).IsNotNull();
                    {{collectionType}} nonNullable = {|#6:values|};
                }

                public async Task WithoutNullAssertionTest({{collectionType}}? values)
                {
                    await Assert.That(values).IsEmpty();
                    Consume({|#7:values|});
                }

                private static void Consume({{collectionType}} values) { }
            }
            """;

        await AnalyzerTestHelpers
            .CreateSuppressorTest<IsNotNullAssertionSuppressor>(code)
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8600, CS8602, CS8604)
            .WithExpectedDiagnosticsResults(
                CS8604.WithLocation(0).WithIsSuppressed(true),
                CS8602.WithLocation(1).WithIsSuppressed(true),
                CS8604.WithLocation(2).WithIsSuppressed(true),
                CS8602.WithLocation(3).WithIsSuppressed(true),
                CS8604.WithLocation(4).WithIsSuppressed(true),
                CS8604.WithLocation(5).WithIsSuppressed(true),
                CS8600.WithLocation(6).WithIsSuppressed(true),
                CS8604.WithLocation(7).WithIsSuppressed(false))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    public async Task Does_Not_Suppress_After_Custom_IsNotNull_Instance_Method()
    {
        const string code = """
            #nullable enable
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Sources;

            public static class CustomAssertions
            {
                public static CustomAssertion Custom(this ListAssertion<int> source) => new CustomAssertion();
                public static CustomDerivedAssertion CustomDerived(this ListAssertion<int> source) => new CustomDerivedAssertion();
            }

            public class CustomAssertion
            {
                public Task IsNotNull() => Task.CompletedTask;
            }

            public class CustomDerivedAssertion : ListAssertion<int>
            {
                public CustomDerivedAssertion() : base(null, null) { }
                public new Task IsNotNull() => Task.CompletedTask;
            }

            public class MyTests
            {
                public async Task TestMethod(IList<int>? values)
                {
                    await Assert.That(values).Custom().IsNotNull();
                    _ = {|#0:values|}.Count;
                }

                public async Task HiddenMethodTest(IList<int>? values)
                {
                    await Assert.That(values).CustomDerived().IsNotNull();
                    _ = {|#1:values|}.Count;
                }
            }
            """;

        await AnalyzerTestHelpers
            .CreateSuppressorTest<IsNotNullAssertionSuppressor>(code)
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8602)
            .WithExpectedDiagnosticsResults(
                CS8602.WithLocation(0).WithIsSuppressed(false),
                CS8602.WithLocation(1).WithIsSuppressed(false))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

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
    public async Task Does_Not_Suppress_CS8602_After_Custom_IsNotNull_Assertion()
    {
        const string code = """
            #nullable enable
            using System.Threading.Tasks;
            using CustomAssertions;
            using TUnit.Assertions;
            using TUnit.Assertions.Sources;

            namespace CustomAssertions
            {
                public static class ValueAssertionExtensions
                {
                    public static Task IsNotNull(this ValueAssertion<string> source) => Task.CompletedTask;
                }
            }

            public class MyTests
            {
                public async Task TestMethod()
                {
                    string? nullableString = GetNullableString();

                    await Assert.That(nullableString).IsNotNull();

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
    public async Task Suppresses_CS8602_After_Should_NotBeNull_Assertion()
    {
        const string code = """
            #nullable enable
            using System.Threading.Tasks;
            using TUnit.Assertions.Should;
            using TUnit.Assertions.Should.Extensions;

            public class MyTests
            {
                public async Task TestMethod()
                {
                    string? nullableString = GetNullableString();

                    await nullableString.Should().NotBeNull();

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
    public async Task Does_Not_Suppress_CS8602_After_Should_Assertion_Without_NotBeNull()
    {
        const string code = """
            #nullable enable
            using System.Threading.Tasks;
            using TUnit.Assertions.Should;
            using TUnit.Assertions.Should.Extensions;

            public class MyTests
            {
                public async Task TestMethod()
                {
                    string? nullableString = GetNullableString();

                    await nullableString.Should().BeNull();

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
    public async Task Does_Not_Suppress_CS8602_After_Unrelated_Should_NotBeNull_Assertion()
    {
        const string code = """
            #nullable enable
            using System.Threading.Tasks;
            using OtherLibrary;

            namespace OtherLibrary
            {
                public sealed class Wrapper
                {
                    public Task NotBeNull() => Task.CompletedTask;
                }

                public static class ShouldExtensions
                {
                    public static Wrapper Should(this string? value) => new();
                }
            }

            public class MyTests
            {
                public async Task TestMethod()
                {
                    string? nullableString = GetNullableString();

                    await nullableString.Should().NotBeNull();

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
    public async Task Does_Not_Suppress_CS8602_After_Custom_NotBeNull_Assertion()
    {
        const string code = """
            #nullable enable
            using System.Threading.Tasks;
            using CustomAssertions;
            using TUnit.Assertions.Should;
            using TUnit.Assertions.Should.Core;

            namespace CustomAssertions
            {
                public static class ShouldSourceExtensions
                {
                    public static Task NotBeNull(this ShouldSource<string> source) => Task.CompletedTask;
                }
            }

            public class MyTests
            {
                public async Task TestMethod()
                {
                    string? nullableString = GetNullableString();

                    await nullableString.Should().NotBeNull();

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
    public async Task Suppresses_CS8602_After_Should_NotBeNull_In_Assertion_Chains()
    {
        const string code = """
            #nullable enable
            using System.Threading.Tasks;
            using TUnit.Assertions.Should;
            using TUnit.Assertions.Should.Extensions;

            public class MyTests
            {
                public async Task TestMethod()
                {
                    string? notBeNullFirst = GetNullableString();
                    string? notBeNullLast = GetNullableString();

                    await notBeNullFirst.Should().NotBeNull().And.Contain("test");
                    await notBeNullLast.Should().Contain("test").And.NotBeNull();

                    var firstLength = {|#0:notBeNullFirst|}.Length;
                    var lastLength = {|#1:notBeNullLast|}.Length;
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
                // Only the first usage generates a warning; subsequent uses benefit from flow analysis
                CS8602.WithLocation(0).WithIsSuppressed(true)
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

    [Test]
    public async Task Suppresses_CS8629_After_IsNotNull_Assertion_On_Simple_Nullable_Value_Type()
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
                    int? nullableInt = GetNullableInt();

                    await Assert.That(nullableInt).IsNotNull();

                    // This would normally produce CS8629: Nullable value type may be null
                    // But the suppressor should suppress it after IsNotNull assertion
                    int value = {|#0:nullableInt|}.Value;
                }

                private int? GetNullableInt() => 42;
            }
            """;

        await AnalyzerTestHelpers
            .CreateSuppressorTest<IsNotNullAssertionSuppressor>(code)
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8629)
            .WithExpectedDiagnosticsResults(CS8629.WithLocation(0).WithIsSuppressed(true))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    public async Task Does_Not_Suppress_CS8629_Without_IsNotNull_Assertion()
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
                    int? nullableInt = GetNullableInt();

                    // No IsNotNull assertion here

                    // This should still produce CS8629 warning
                    int value = {|#0:nullableInt|}.Value;
                }

                private int? GetNullableInt() => 42;
            }
            """;

        await AnalyzerTestHelpers
            .CreateSuppressorTest<IsNotNullAssertionSuppressor>(code)
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8629)
            .WithExpectedDiagnosticsResults(CS8629.WithLocation(0).WithIsSuppressed(false))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    public async Task Suppresses_CS8629_On_Member_Access_Nullable_Value_Type()
    {
        const string code = """
            #nullable enable
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Extensions;

            public class MyTests
            {
                public async Task TestMethod(int? id)
                {
                    var value = new { Id = id };

                    await Assert.That(value.Id).IsNotNull();

                    // This would normally produce CS8629: Nullable value type may be null
                    // But the suppressor should suppress it after IsNotNull assertion on value.Id
                    int idValue = {|#0:value.Id|}.Value;
                }
            }
            """;

        await AnalyzerTestHelpers
            .CreateSuppressorTest<IsNotNullAssertionSuppressor>(code)
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8629)
            .WithExpectedDiagnosticsResults(CS8629.WithLocation(0).WithIsSuppressed(true))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    public async Task Suppresses_Inside_Lambda()
    {
        const string code = """
            #nullable enable
            using System;
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Extensions;

            public class MyTests
            {
                public async Task TestMethod()
                {
                    string? nullableString = GetNullableString();

                    Func<Task> act = async () =>
                    {
                        await Assert.That(nullableString).IsNotNull();

                        // Should be suppressed inside a lambda
                        var length = {|#0:nullableString|}.Length;
                    };

                    await act();
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
    public async Task Suppresses_Inside_Local_Function()
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

                    async Task LocalFunc()
                    {
                        await Assert.That(nullableString).IsNotNull();

                        // Should be suppressed inside a local function
                        var length = {|#0:nullableString|}.Length;
                    }

                    await LocalFunc();
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
    public async Task Suppresses_CS8629_On_Named_Type_Member_Access()
    {
        const string code = """
            #nullable enable
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Extensions;

            public class MyModel
            {
                public int? Id { get; set; }
            }

            public class MyTests
            {
                public async Task TestMethod()
                {
                    var model = GetModel();

                    await Assert.That(model.Id).IsNotNull();

                    // This would normally produce CS8629: Nullable value type may be null
                    // But the suppressor should suppress it after IsNotNull assertion on model.Id
                    int idValue = {|#0:model.Id|}.Value;
                }

                private MyModel GetModel() => new MyModel { Id = 42 };
            }
            """;

        await AnalyzerTestHelpers
            .CreateSuppressorTest<IsNotNullAssertionSuppressor>(code)
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8629)
            .WithExpectedDiagnosticsResults(CS8629.WithLocation(0).WithIsSuppressed(true))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }
}
