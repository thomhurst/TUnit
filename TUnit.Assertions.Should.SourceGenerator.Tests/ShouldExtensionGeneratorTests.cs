using System.Collections.Immutable;
using System.Runtime.CompilerServices;
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
    public async Task Overload_with_trailing_optional_constructor_parameter_is_emitted()
    {
        var output = await RunGenerator("""
            using System;
            using System.Runtime.CompilerServices;
            using TUnit.Assertions.Core;

            namespace MyNamespace;

            public class ExceptionMessageEqualsAssertion<TException> : Assertion<TException>
                where TException : Exception
            {
                public ExceptionMessageEqualsAssertion(
                    AssertionContext<TException> context,
                    string expectedMessage,
                    StringComparison comparison = StringComparison.Ordinal)
                    : base(context) { }
                protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TException> metadata)
                    => Task.FromResult(AssertionResult.Passed);
                protected override string GetExpectation() => "to have message";
            }

            public static class ExceptionExtensions
            {
                public static ExceptionMessageEqualsAssertion<TException> WithMessage<TException>(
                    this IAssertionSource<TException> source,
                    string expectedMessage,
                    [CallerArgumentExpression(nameof(expectedMessage))] string? expression = null)
                    where TException : Exception
                    => new(source.Context, expectedMessage);

                public static ExceptionMessageEqualsAssertion<TException> WithMessage<TException>(
                    this IAssertionSource<TException> source,
                    string expectedMessage,
                    StringComparison comparison,
                    [CallerArgumentExpression(nameof(expectedMessage))] string? expression = null)
                    where TException : Exception
                    => new(source.Context, expectedMessage, comparison);
            }
            """);

        await Assert.That(output).Contains("WithMessage<TException>(this global::TUnit.Assertions.Should.Core.IShouldSource<TException> source, string expectedMessage, [global::System.Runtime.CompilerServices.CallerArgumentExpression(\"expectedMessage\")] string? expression = null)");
        await Assert.That(output).Contains("WithMessage<TException>(this global::TUnit.Assertions.Should.Core.IShouldSource<TException> source, string expectedMessage, System.StringComparison comparison, [global::System.Runtime.CompilerServices.CallerArgumentExpression(\"expectedMessage\")] string? expression = null)");
        await Assert.That(output).Contains("new global::MyNamespace.ExceptionMessageEqualsAssertion<TException>(innerContext, expectedMessage)");
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
    public async Task Obsolete_attribute_is_forwarded()
    {
        var output = await RunGenerator("""
            using System;
            using TUnit.Assertions.Core;

            namespace MyNamespace;

            public class LegacyAssertion : Assertion<int>
            {
                public LegacyAssertion(AssertionContext<int> context) : base(context) { }
                protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<int> metadata)
                    => Task.FromResult(AssertionResult.Passed);
                protected override string GetExpectation() => "to be legacy";
            }

            public static class LegacyExtensions
            {
                [Obsolete("Use IsModern instead.")]
                public static LegacyAssertion IsLegacy(this IAssertionSource<int> source)
                    => new(source.Context);
            }
            """);

        await Assert.That(output).Contains("Obsolete");
        await Assert.That(output).Contains("Use IsModern instead.");
    }

    [Test]
    public async Task Wrapper_generation_deduplicates_overridden_instance_methods()
    {
        var output = await RunGenerator("""
            using TUnit.Assertions.Core;
            using TUnit.Assertions.Should.Attributes;
            using TUnit.Assertions.Should.Core;

            namespace MyNamespace;

            public class BaseWrappedAssertion : Assertion<int>
            {
                public BaseWrappedAssertion(AssertionContext<int> context) : base(context) { }
                public virtual BaseWrappedAssertion IsReady() => new(Context);
                protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<int> metadata)
                    => Task.FromResult(AssertionResult.Passed);
                protected override string GetExpectation() => "to be ready";
            }

            public sealed class DerivedWrappedAssertion : BaseWrappedAssertion
            {
                public DerivedWrappedAssertion(AssertionContext<int> context) : base(context) { }
                public override BaseWrappedAssertion IsReady() => new(Context);
            }

            [ShouldGeneratePartial(typeof(DerivedWrappedAssertion))]
            public sealed partial class ShouldDerivedSource : IShouldSource<int>
            {
                public AssertionContext<int> Context { get; }
                public ShouldDerivedSource(AssertionContext<int> context) => Context = context;
                string? IShouldSource<int>.ConsumeBecauseMessage() => null;
            }
            """);

        await Assert.That(CountOccurrences(output, " BeReady(")).IsEqualTo(1);
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

    [Test]
    public async Task Assert_That_dictionary_specialization_emits_matching_Should_overload()
    {
        var output = await RunGenerator("""
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            using TUnit.Assertions.Core;

            namespace TUnit.Assertions;

            public class DictionaryAssertion<TKey, TValue> : IAssertionSource<IReadOnlyDictionary<TKey, TValue>>
                where TKey : notnull
            {
                public AssertionContext<IReadOnlyDictionary<TKey, TValue>> Context { get; }
                public DictionaryAssertion(IReadOnlyDictionary<TKey, TValue>? value, string? expression)
                    => Context = new AssertionContext<IReadOnlyDictionary<TKey, TValue>>(value!, new System.Text.StringBuilder());
                public TypeOfAssertion<IReadOnlyDictionary<TKey, TValue>, TExpected> IsTypeOf<TExpected>() => throw new System.NotImplementedException();
                public IsNotTypeOfAssertion<IReadOnlyDictionary<TKey, TValue>, TExpected> IsNotTypeOf<TExpected>() => throw new System.NotImplementedException();
                public IsAssignableToAssertion<TExpected, IReadOnlyDictionary<TKey, TValue>> IsAssignableTo<TExpected>() => throw new System.NotImplementedException();
                public IsNotAssignableToAssertion<TExpected, IReadOnlyDictionary<TKey, TValue>> IsNotAssignableTo<TExpected>() => throw new System.NotImplementedException();
                public IsAssignableFromAssertion<TExpected, IReadOnlyDictionary<TKey, TValue>> IsAssignableFrom<TExpected>() => throw new System.NotImplementedException();
                public IsNotAssignableFromAssertion<TExpected, IReadOnlyDictionary<TKey, TValue>> IsNotAssignableFrom<TExpected>() => throw new System.NotImplementedException();
            }

            public static class Assert
            {
                [OverloadResolutionPriority(2)]
                public static DictionaryAssertion<TKey, TValue> That<TKey, TValue>(
                    IReadOnlyDictionary<TKey, TValue>? value,
                    [CallerArgumentExpression(nameof(value))] string? expression = null)
                    where TKey : notnull
                    => new(value, expression);
            }
            """);

        await Assert.That(output).Contains("public static global::TUnit.Assertions.Should.Core.ShouldSource<System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>> Should<TKey, TValue>(this System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>? value");
        await Assert.That(output).Contains("Assert.That<TKey, TValue>(value, expression)");
        await Assert.That(output).Contains("Append(expression ?? \"?\").Append(\".Should()\")");
    }

    [Test]
    public async Task Assert_That_set_specialization_emits_matching_Should_overload()
    {
        var output = await RunGenerator("""
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            using TUnit.Assertions.Core;

            namespace TUnit.Assertions;

            public class SetAssertion<TItem> : IAssertionSource<ISet<TItem>>
            {
                public AssertionContext<ISet<TItem>> Context { get; }
                public SetAssertion(ISet<TItem>? value, string? expression)
                    => Context = new AssertionContext<ISet<TItem>>(value!, new System.Text.StringBuilder());
                public TypeOfAssertion<ISet<TItem>, TExpected> IsTypeOf<TExpected>() => throw new System.NotImplementedException();
                public IsNotTypeOfAssertion<ISet<TItem>, TExpected> IsNotTypeOf<TExpected>() => throw new System.NotImplementedException();
                public IsAssignableToAssertion<TExpected, ISet<TItem>> IsAssignableTo<TExpected>() => throw new System.NotImplementedException();
                public IsNotAssignableToAssertion<TExpected, ISet<TItem>> IsNotAssignableTo<TExpected>() => throw new System.NotImplementedException();
                public IsAssignableFromAssertion<TExpected, ISet<TItem>> IsAssignableFrom<TExpected>() => throw new System.NotImplementedException();
                public IsNotAssignableFromAssertion<TExpected, ISet<TItem>> IsNotAssignableFrom<TExpected>() => throw new System.NotImplementedException();
            }

            public static class Assert
            {
                [OverloadResolutionPriority(3)]
                public static SetAssertion<TItem> That<TItem>(
                    ISet<TItem>? value,
                    [CallerArgumentExpression(nameof(value))] string? expression = null)
                    => new(value, expression);
            }
            """);

        await Assert.That(output).Contains("public static global::TUnit.Assertions.Should.Core.ShouldSource<System.Collections.Generic.ISet<TItem>> Should<TItem>(this System.Collections.Generic.ISet<TItem>? value");
        await Assert.That(output).Contains("OverloadResolutionPriority(3)");
        await Assert.That(output).Contains("Assert.That<TItem>(value, expression)");
    }

    /// <summary>
    /// Compiles <paramref name="userSource"/> with the Should-generator's input dependencies,
    /// runs <see cref="ShouldExtensionGenerator"/>, snapshots the full generated source via
    /// Verify (per-TFM <c>.verified.txt</c> files), and returns the concatenated output so
    /// callers can additionally string-match key tokens. The Verify snapshot catches
    /// formatting/ordering/<c>global::</c> regressions that token-level checks miss; the
    /// inline <c>Contains</c> assertions remain as explicit guard-rails for the user-visible
    /// API tokens that any change should call out deliberately.
    /// </summary>
    private static async Task<string> RunGenerator(string userSource, [CallerMemberName] string testName = "")
    {
        // On net8.0 hosts, OverloadResolutionPriorityAttribute is missing from the BCL and the
        // Polyfill copy compiled into this test assembly is internal — invisible to the synthetic
        // GeneratorTest compilation. Inject a public copy so the attribute resolves consistently
        // across all TFMs the test multi-targets.
        var inputTrees = new List<SyntaxTree> { CSharpSyntaxTree.ParseText(userSource) };
#if NET8_0
        inputTrees.Add(CSharpSyntaxTree.ParseText("""
            namespace System.Runtime.CompilerServices;
            [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Constructor | System.AttributeTargets.Property, Inherited = false)]
            public sealed class OverloadResolutionPriorityAttribute : System.Attribute
            {
                public OverloadResolutionPriorityAttribute(int priority) => Priority = priority;
                public int Priority { get; }
            }
            """));
#endif

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTest",
            syntaxTrees: inputTrees,
            references: GetReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new ShouldExtensionGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var updatedCompilation, out var diagnostics);

        await Assert.That(diagnostics.Length).IsEqualTo(0)
            .Because("Generator should not emit diagnostics for valid input");

        var generatedTrees = updatedCompilation.SyntaxTrees
            .Where(t => !compilation.SyntaxTrees.Contains(t))
            .Select(t => t.ToString());

        var combined = string.Join("\n//------\n", generatedTrees);

        await Verify(combined)
            .UseFileName($"{nameof(ShouldExtensionGeneratorTests)}.{testName}")
            .UniqueForTargetFrameworkAndVersion();

        return combined;
    }

    private static IEnumerable<MetadataReference> GetReferences()
    {
        // Mirror the loaded assemblies of the test process. Works on both .NET Core
        // (where TRUSTED_PLATFORM_ASSEMBLIES populates this set) and .NET Framework,
        // unlike <c>AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")</c> which returns
        // null on .NET Framework and would leave the in-memory compilation without BCL
        // references — making symbol resolution silently fail in the generator.
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.IsDynamic || string.IsNullOrWhiteSpace(asm.Location))
            {
                continue;
            }
            yield return MetadataReference.CreateFromFile(asm.Location);
        }

        yield return MetadataReference.CreateFromFile(typeof(Assertion<>).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(TUnit.Assertions.Should.Core.IShouldSource).Assembly.Location);
    }

    private static int CountOccurrences(string value, string search)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += search.Length;
        }
        return count;
    }
}
