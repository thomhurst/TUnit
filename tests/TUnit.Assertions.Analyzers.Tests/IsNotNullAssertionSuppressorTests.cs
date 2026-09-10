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
    [Arguments("Assert.That(value).IsNotNull()")]
    [Arguments("value.Should().NotBeNull()")]
    public async Task Does_Not_Suppress_For_Source_Defined_TUnit_Lookalikes(string assertion)
    {
        var code = $$"""
            #nullable enable
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Extensions;
            using TUnit.Assertions.Should;
            using TUnit.Assertions.Should.Extensions;

            namespace TUnit.Assertions
            {
                public class FakeSource { }
                public static class Assert
                {
                    public static FakeSource That<T>(T? value) => new FakeSource();
                }
            }

            namespace TUnit.Assertions.Extensions
            {
                public static class AssertionExtensions
                {
                    public static Task IsNotNull(this FakeSource source) => Task.CompletedTask;
                }
            }

            namespace TUnit.Assertions.Should
            {
                public static class ShouldExtensions
                {
                    public static FakeSource Should(this string? value) => new FakeSource();
                }
            }

            namespace TUnit.Assertions.Should.Extensions
            {
                public static class ShouldAssertionExtensions
                {
                    public static Task NotBeNull(this FakeSource source) => Task.CompletedTask;
                }
            }

            public class MyTests
            {
                public async Task TestMethod(string? value)
                {
                    await {{assertion}};
                    _ = {|#0:value|}.Length;
                }
            }
            """;

        await AnalyzerTestHelpers
            .CreateSuppressorTest<IsNotNullAssertionSuppressor>(code)
            .IgnoringDiagnostics("CS1591", "CS0436")
            .WithSpecificDiagnostics(CS8602)
            .WithExpectedDiagnosticsResults(CS8602.WithLocation(0).WithIsSuppressed(false))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

    [Test]
    [Arguments("IAsyncEnumerable<int>", true)]
    [Arguments("Task", true)]
    [Arguments("Func<Task>", true)]
    [Arguments("IAsyncEnumerable<int>", false)]
    [Arguments("Task", false)]
    [Arguments("Func<Task>", false)]
    public async Task Recognizes_Async_IsNotNull_Instance_Methods(string valueType, bool assertNotNull)
    {
        var nullAssertion = assertNotNull ? "IsNotNull" : "IsNull";
        var code = $$"""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Extensions;

            public class MyTests
            {
                public async Task TestMethod({{valueType}}? value)
                {
                    await Assert.That({|#0:value|}).{{nullAssertion}}();
                    _ = value.GetHashCode();
                }
            }
            """;

        // The async overloads take non-nullable parameters: CS8604 occurs at That,
        // then the compiler promotes value's null state, so there is no later CS8602.
        // Use the runtime libraries: the netstandard2.0 Assert lacks the async-enumerable
        // overload. Matching framework references avoid the standard helper's CS1705 issue.
        var test = new AnalyzerTestHelpers.CSharpSuppressorTest<IsNotNullAssertionSuppressor, DefaultVerifier>
        {
            TestCode = code,
#if NET10_0_OR_GREATER
            ReferenceAssemblies = new ReferenceAssemblies("net10.0",
                new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0"), Path.Combine("ref", "net10.0")),
#elif NET9_0_OR_GREATER
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
#else
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
#endif
        };
        test.TestState.AdditionalReferences.AddRange([
            MetadataReference.CreateFromFile(typeof(TUnitAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Assert).Assembly.Location),
#if NET8_0
            MetadataReference.CreateFromFile(TUnit.Tests.Shared.AnalyzerTestCompatibility.GetSystemTextJson9DllPath()),
#endif
        ]);

        await test
            .IgnoringDiagnostics("CS1591")
            .WithSpecificDiagnostics(CS8604)
            .WithExpectedDiagnosticsResults(CS8604.WithLocation(0).WithIsSuppressed(assertNotNull))
            .WithCompilerDiagnostics(CompilerDiagnostics.Warnings)
            .RunAsync();
    }

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
    [Arguments("Assert.That(values).IsNull().Or.IsNotNull()")]
    [Arguments("Assert.That(values).IsNotNull().Or.IsNull()")]
    [Arguments("values.Should().BeNull().Or.NotBeNull()")]
    [Arguments("values.Should().NotBeNull().Or.BeNull()")]
    public async Task Does_Not_Suppress_After_Disjunctive_Null_Check(string assertion)
    {
        var code = $$"""
            #nullable enable
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Extensions;
            using TUnit.Assertions.Should;
            using TUnit.Assertions.Should.Extensions;

            public class MyTests
            {
                public async Task TestMethod(IList<int>? values)
                {
                    await {{assertion}};
                    _ = {|#0:values|}.Count;
                }
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
    [Arguments("Assert.That(model.Or).IsNotNull().Because(nameof(MyTests.Or))")]
    [Arguments("model.Or.Should().NotBeNull().Because(nameof(MyTests.Or))")]
    public async Task Suppresses_When_Or_Is_Outside_The_Assertion_Chain(string assertion)
    {
        var code = $$"""
            #nullable enable
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Extensions;
            using TUnit.Assertions.Should;
            using TUnit.Assertions.Should.Extensions;

            public class MyTests
            {
                public IList<int>? Or { get; set; }

                public async Task TestMethod(MyTests model)
                {
                    await {{assertion}};
                    _ = {|#0:model.Or|}.Count;
                }
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
    public async Task Does_Not_Suppress_After_Custom_Should_Transform()
    {
        const string code = """
            #nullable enable
            using System.Threading.Tasks;
            using TUnit.Assertions.Should;
            using TUnit.Assertions.Should.Core;
            using TUnit.Assertions.Should.Extensions;

            public static class CustomAssertions
            {
                public static ShouldSource<string> CustomTransform(this ShouldSource<string> source) => "other".Should();
            }

            public class MyTests
            {
                public async Task TestMethod(string? value)
                {
                    await value.Should().CustomTransform().NotBeNull();
                    _ = {|#0:value|}.Length;
                }
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
    [Arguments("CustomTransform()")]
    [Arguments("CustomPropertyTransform().Other")]
    public async Task Does_Not_Suppress_After_Custom_Transform_With_BuiltIn_IsNotNull(string transform)
    {
        var code = $$"""
            #nullable enable
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using TUnit.Assertions;
            using TUnit.Assertions.Sources;

            public static class CustomAssertions
            {
                public static OtherAssertion CustomTransform(this ListAssertion<int> source) => new OtherAssertion();
                public static OtherAssertion CustomPropertyTransform(this ListAssertion<int> source) => new OtherAssertion();
            }

            public class OtherAssertion : ListAssertion<int>
            {
                public OtherAssertion() : base(new List<int>(), null) { }
                public ListAssertion<int> Other => new ListAssertion<int>(new List<int>(), null);
            }

            public class MyTests
            {
                public async Task TestMethod(IList<int>? values)
                {
                    await Assert.That(values).{{transform}}.IsNotNull();
                    _ = {|#0:values|}.Count;
                }
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
    public async Task Does_Not_Suppress_After_IsNotNull_With_Or_Chain()
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

                    // IsNotNull is optional in an Or chain, so retain the warning.
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
