using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Should.SourceGenerator.Tests;

/// <summary>
/// Locks in the key behaviours of <see cref="ShouldExtensionGenerator"/> by compiling small
/// inline snippets, running the generator, and asserting on the emitted source. Failing tests
/// here mean the Should-flavored API surface has shifted in a user-visible way — any change
/// is intentional and should be reflected in the assertions below.
/// </summary>
/// <remarks>
/// The generator scans for extension methods on <see cref="IAssertionSource{T}"/>; the test
/// snippets declare those methods explicitly because <c>TUnit.Assertions.SourceGenerator</c>
/// (which would normally synthesise them from <c>[AssertionExtension]</c>) doesn't run inside
/// the test's in-memory compilation.
/// </remarks>
public class ShouldExtensionGeneratorTests
{
    [Test]
    public async Task SimpleAssertionExtension_emits_conjugated_extension_method()
    {
        var output = await RunGenerator("""
            using TUnit.Assertions.Core;

            namespace MyNamespace;

            public class StringIsEmptyAssertion : Assertion<string>
            {
                public StringIsEmptyAssertion(AssertionContext<string> context) : base(context) { }
                protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
                    => Task.FromResult(AssertionResult.Passed);
                protected override string GetExpectation() => "to be empty";
            }

            public static class StringIsEmptyExtensions
            {
                public static StringIsEmptyAssertion IsEmpty(this IAssertionSource<string> source)
                    => new(source.Context);
            }
            """);

        // Conjugation: IsEmpty -> BeEmpty (Is* -> Be* rule).
        await Assert.That(output).Contains("BeEmpty");
        // Should-flavored surface — first param is IShouldSource<T>, return is ShouldAssertion<T>.
        await Assert.That(output).Contains("this global::TUnit.Assertions.Should.Core.IShouldSource<string> source");
        await Assert.That(output).Contains("global::TUnit.Assertions.Should.Core.ShouldAssertion<string>");
        // Inner assertion is constructed with the existing TUnit.Assertions class.
        await Assert.That(output).Contains("new global::MyNamespace.StringIsEmptyAssertion(innerContext)");
    }

    [Test]
    public async Task GenericAssertionExtension_emits_method_generic_param()
    {
        var output = await RunGenerator("""
            using TUnit.Assertions.Core;
            using System.Runtime.CompilerServices;

            namespace MyNamespace;

            public class MyEqualsAssertion<TValue> : Assertion<TValue>
            {
                public MyEqualsAssertion(AssertionContext<TValue> context, TValue expected) : base(context) { }
                protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
                    => Task.FromResult(AssertionResult.Passed);
                protected override string GetExpectation() => "to be equal";
            }

            public static class MyEqualsExtensions
            {
                public static MyEqualsAssertion<TValue> IsEqualTo<TValue>(
                    this IAssertionSource<TValue> source,
                    TValue expected,
                    [CallerArgumentExpression(nameof(expected))] string? expectedExpression = null)
                    => new(source.Context, expected);
            }
            """);

        await Assert.That(output).Contains("BeEqualTo<TValue>");
        await Assert.That(output).Contains("IShouldSource<TValue>");
        await Assert.That(output).Contains("TValue expected");
    }

    [Test]
    public async Task ShouldNameAttribute_overrides_conjugation()
    {
        var output = await RunGenerator("""
            using TUnit.Assertions.Core;
            using TUnit.Assertions.Should.Attributes;

            namespace MyNamespace;

            [ShouldName("BeAnOddNumber")]
            public class OddAssertion : Assertion<int>
            {
                public OddAssertion(AssertionContext<int> context) : base(context) { }
                protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<int> metadata)
                    => Task.FromResult(AssertionResult.Passed);
                protected override string GetExpectation() => "to be odd";
            }

            public static class OddExtensions
            {
                public static OddAssertion IsOdd(this IAssertionSource<int> source)
                    => new(source.Context);
            }
            """);

        await Assert.That(output).Contains("BeAnOddNumber");
        // Default conjugation would have produced "BeOdd" — verify it didn't sneak in alongside.
        await Assert.That(output).DoesNotContain("public static global::TUnit.Assertions.Should.Core.ShouldAssertion<int> BeOdd(");
    }

    [Test]
    public async Task IsNot_prefix_conjugates_to_NotBe()
    {
        var output = await RunGenerator("""
            using TUnit.Assertions.Core;

            namespace MyNamespace;

            public class NotEmptyAssertion : Assertion<string>
            {
                public NotEmptyAssertion(AssertionContext<string> context) : base(context) { }
                protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
                    => Task.FromResult(AssertionResult.Passed);
                protected override string GetExpectation() => "to not be empty";
            }

            public static class NotEmptyExtensions
            {
                public static NotEmptyAssertion IsNotEmpty(this IAssertionSource<string> source)
                    => new(source.Context);
            }
            """);

        await Assert.That(output).Contains("NotBeEmpty");
    }

    [Test]
    public async Task CallerArgumentExpression_attribute_is_propagated()
    {
        var output = await RunGenerator("""
            using TUnit.Assertions.Core;
            using System.Runtime.CompilerServices;

            namespace MyNamespace;

            public class MyBetweenAssertion<TValue> : Assertion<TValue>
            {
                public MyBetweenAssertion(AssertionContext<TValue> context, TValue min, TValue max) : base(context) { }
                protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
                    => Task.FromResult(AssertionResult.Passed);
                protected override string GetExpectation() => "to be between";
            }

            public static class MyBetweenExtensions
            {
                public static MyBetweenAssertion<TValue> IsBetween<TValue>(
                    this IAssertionSource<TValue> source,
                    TValue min,
                    TValue max,
                    [CallerArgumentExpression(nameof(min))] string? minExpression = null,
                    [CallerArgumentExpression(nameof(max))] string? maxExpression = null)
                    => new(source.Context, min, max);
            }
            """);

        await Assert.That(output).Contains("BeBetween");
        await Assert.That(output).Contains("CallerArgumentExpression(\"min\")");
        await Assert.That(output).Contains("CallerArgumentExpression(\"max\")");
    }

    /// <summary>
    /// Compiles <paramref name="userSource"/> together with the Should-generator's input
    /// dependencies, runs <see cref="ShouldExtensionGenerator"/>, and returns the concatenated
    /// generated source so callers can string-match key tokens. Asserts that the generator emits
    /// no diagnostics for valid input.
    /// </summary>
    private static async Task<string> RunGenerator(string userSource)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTest",
            syntaxTrees: [CSharpSyntaxTree.ParseText(userSource)],
            references: GetReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new ShouldExtensionGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var updatedCompilation, out var diagnostics);

        await Assert.That(diagnostics.Length).IsEqualTo(0)
            .Because("Generator should not emit diagnostics for valid input");

        var trees = updatedCompilation.SyntaxTrees
            .Where(t => t != compilation.SyntaxTrees[0])
            .Select(t => t.ToString());

        return string.Join("\n//------\n", trees);
    }

    private static IEnumerable<MetadataReference> GetReferences()
    {
        // Trust assembly references from the running test process — they include all of
        // TUnit.Assertions, TUnit.Assertions.Should, and the BCL.
        var trustedAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?.Split(Path.PathSeparator)
            ?? [];
        foreach (var path in trustedAssemblies)
        {
            yield return MetadataReference.CreateFromFile(path);
        }

        yield return MetadataReference.CreateFromFile(typeof(Assertion<>).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(TUnit.Assertions.Should.Core.IShouldSource).Assembly.Location);
    }
}
