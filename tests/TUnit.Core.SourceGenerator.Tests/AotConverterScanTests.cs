using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

public class AotConverterScanTests
{
    [Test]
    public async Task ExplicitDecimalConverterTakesPriority()
    {
        // This assembly has no decimal data sources, so the converter is registered manually.
        TUnit.Core.Converters.AotConverterRegistry.Register<decimal, int>(value => (int)value);
        await Assert.That(TUnit.Core.Helpers.CastHelper.Cast<int>(1.9m)).IsEqualTo(1);
    }

    [Test]
    public async Task PreservesBuiltInDecimalConversions()
    {
        const string source = """
            using TUnit.Core;
            public class Tests
            {
                [Test, Arguments('A')]
                public void Decimal(decimal value) { }
            }
            """;
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("DecimalConverters",
            [CSharpSyntaxTree.ParseText(source, parseOptions)], ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new AotConverterGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        await Assert.That(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        var generated = string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(t => t.ToString()));
        await Assert.That(System.Text.RegularExpressions.Regex.IsMatch(generated, @"SourceType => typeof\(char\);\s+public Type TargetType => typeof\(decimal\);")).IsTrue();
        await Assert.That(System.Text.RegularExpressions.Regex.IsMatch(generated, @"SourceType => typeof\(decimal\);\s+public Type TargetType => typeof\(char\);")).IsTrue();
        await Assert.That(generated.Contains("int converted = (int)global::System.Convert.ChangeType(sourceTypedValue, typeof(int));")).IsTrue();
    }

    [Test]
    public async Task GeneratesParseConversionsFromString()
    {
        const string source = """
            using System;
            using TUnit.Core;
            public class Tests
            {
                [Test, Arguments("a", "b", "c", "d", "e", "f", "g")]
                public void Parse(Guid guid, TimeSpan? timeSpan, DateTime dateTime, nint nativeInt, nuint nativeUInt, int number, string text) { }
            }
            """;
        var generated = await GenerateTypeConverters(source);

#if NET
        await Assert.That(generated).Contains("global::System.Guid converted = global::System.Guid.Parse(sourceTypedValue, global::System.Globalization.CultureInfo.InvariantCulture);");
        await Assert.That(generated).Contains("nint converted = nint.Parse(sourceTypedValue, global::System.Globalization.CultureInfo.InvariantCulture);");
        await Assert.That(generated).Contains("nuint converted = nuint.Parse(sourceTypedValue, global::System.Globalization.CultureInfo.InvariantCulture);");
#else
        // .NET Framework only has Guid.Parse(string).
        await Assert.That(generated).Contains("global::System.Guid converted = global::System.Guid.Parse(sourceTypedValue);");
#endif
        await Assert.That(generated).Contains("global::System.TimeSpan converted = global::System.TimeSpan.Parse(sourceTypedValue, global::System.Globalization.CultureInfo.InvariantCulture);");
        await Assert.That(generated).Contains("global::System.DateTime converted = global::System.DateTime.Parse(sourceTypedValue, global::System.Globalization.CultureInfo.InvariantCulture);");
        // Primitives keep their IConvertible path.
        await Assert.That(generated).DoesNotContain(" int.Parse(");
        await Assert.That(generated).DoesNotContain(" string.Parse(");
    }

#if NET
    [Test]
    public async Task GeneratesParseConversionsOnlyWhereParseIsCallable()
    {
        const string source = """
            using System;
            using TUnit.Core;
            public readonly struct Public : IParsable<Public>
            {
                public static Public Parse(string s, IFormatProvider? provider) => default;
                public static bool TryParse(string? s, IFormatProvider? provider, out Public result) { result = default; return true; }
            }
            public readonly struct Explicit : IParsable<Explicit>
            {
                static Explicit IParsable<Explicit>.Parse(string s, IFormatProvider? provider) => default;
                static bool IParsable<Explicit>.TryParse(string? s, IFormatProvider? provider, out Explicit result) { result = default; return true; }
            }
            public readonly struct Forbidden : IParsable<Forbidden>
            {
                [Obsolete("x", true)] public static Forbidden Parse(string s, IFormatProvider? provider) => default;
                public static bool TryParse(string? s, IFormatProvider? provider, out Forbidden result) { result = default; return true; }
            }
            public readonly struct ByRef : IParsable<ByRef>
            {
                public static ByRef Parse(ref string s, IFormatProvider? provider) => default;
                static ByRef IParsable<ByRef>.Parse(string s, IFormatProvider? provider) => default;
                static bool IParsable<ByRef>.TryParse(string? s, IFormatProvider? provider, out ByRef result) { result = default; return true; }
            }
            public readonly struct GenericMethod : IParsable<GenericMethod>
            {
                public static GenericMethod Parse<T>(string s, IFormatProvider? provider) => default;
                static GenericMethod IParsable<GenericMethod>.Parse(string s, IFormatProvider? provider) => default;
                static bool IParsable<GenericMethod>.TryParse(string? s, IFormatProvider? provider, out GenericMethod result) { result = default; return true; }
            }
            public readonly struct WithOperator : IParsable<WithOperator>
            {
                public static WithOperator Parse(string s, IFormatProvider? provider) => default;
                public static bool TryParse(string? s, IFormatProvider? provider, out WithOperator result) { result = default; return true; }
                public static implicit operator WithOperator(string value) => default;
            }
            public readonly struct Box<T> : IParsable<Box<T>>
            {
                public static Box<T> Parse(string s, IFormatProvider? provider) => default;
                public static bool TryParse(string? s, IFormatProvider? provider, out Box<T> result) { result = default; return true; }
            }
            public class Tests
            {
                [Test]
                public void Generic<T>(Box<T> box) { }
                [Test, Arguments("a", "b", "c", "d", "e", "f")]
                public void Parse(Public a, Explicit b, Forbidden c, WithOperator d, ByRef e, GenericMethod f) { }
            }
            """;
        var generated = await GenerateTypeConverters(source);

        await Assert.That(generated).Contains("global::Public converted = global::Public.Parse(sourceTypedValue, global::System.Globalization.CultureInfo.InvariantCulture);");
        await Assert.That(generated).DoesNotContain("Explicit.Parse(");
        await Assert.That(generated).DoesNotContain("Forbidden.Parse(");
        await Assert.That(generated).DoesNotContain("WithOperator.Parse(");
        await Assert.That(generated).DoesNotContain("ByRef.Parse(");
        await Assert.That(generated).DoesNotContain("GenericMethod.Parse(");
        await Assert.That(generated).DoesNotContain("global::Box<");
    }
#endif

    [Test]
    public async Task GeneratesTypeConverterConversionsFromString()
    {
        const string source = """
            using System;
            using System.ComponentModel;
            using System.Globalization;
            using TUnit.Core;
            public abstract class BaseConverter : TypeConverter
            {
                public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) => null;
            }
            [TypeConverter(typeof(PlainConverter))]
            public class Plain { }
            [TypeConverter(typeof(TypedConverter))]
            public struct Typed { }
            [TypeConverter("NamedConverter, TypeConverters")]
            public class Named { }
            [TypeConverter("NamedConverter, MissingAssembly")]
            public class WrongAssembly { }
            [TypeConverter("GenericConverter`1")]
            public class GenericByName { }
            [TypeConverter(typeof(PlainConverter))]
            public class WithOperator { public static implicit operator WithOperator(string value) => new(); }
            [TypeConverter(typeof(PlainConverter))]
            public class Box<T> { }
            [TypeConverter(typeof(NoConvertFromConverter))]
            public class NoConvertFrom { }
            [TypeConverter(typeof(UnusableConstructorsConverter))]
            public class UnusableConstructors { }
            [TypeConverter(typeof(ObsoleteOverrideConverter))]
            public class ObsoleteOverride { }
            public class PlainConverter : BaseConverter { }
            public class TypedConverter : BaseConverter
            {
                public TypedConverter(Type type) { }
                public TypedConverter() { }
            }
            public class NamedConverter : BaseConverter { }
            public class GenericConverter<T> : BaseConverter { }
            public class NoConvertFromConverter : TypeConverter { }
            public class UnusableConstructorsConverter : BaseConverter
            {
                public UnusableConstructorsConverter(ref Type type) { }
                [Obsolete("x", true)] public UnusableConstructorsConverter() { }
            }
            public class ObsoleteOverrideConverter : TypeConverter
            {
                [Obsolete("x", true)]
                public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) => null;
            }
            public class Tests
            {
                public Tests(Named named) { }
                [Test, Arguments("a", "b", "c", "d")]
                public void Convert(Plain plain, Typed? typed, WithOperator withOperator, ObsoleteOverride obsoleteOverride) { }
                [Test]
                public void Unsupported(WrongAssembly a, GenericByName b, NoConvertFrom c, UnusableConstructors d) { }
                [Test]
                public void Generic<T>(Box<T> box) { }
            }
            """;
        var generated = await GenerateTypeConverters(source);
        await Assert.That(generated).Contains("global::Plain converted = (global::Plain)new global::PlainConverter().ConvertFrom(null, global::System.Globalization.CultureInfo.InvariantCulture, sourceTypedValue)!;");
        await Assert.That(generated).Contains("global::Typed converted = (global::Typed)new global::TypedConverter(typeof(global::Typed)).ConvertFrom(null, global::System.Globalization.CultureInfo.InvariantCulture, sourceTypedValue)!;");
        await Assert.That(generated).Contains("global::Named converted = (global::Named)new global::NamedConverter().ConvertFrom(");
        // An obsolete override is still callable: the call binds to the non-obsolete TypeConverter.ConvertFrom.
        await Assert.That(generated).Contains("(global::ObsoleteOverride)new global::ObsoleteOverrideConverter().ConvertFrom(");
        // Language conversions keep precedence over the converter.
        await Assert.That(generated).DoesNotContain("(global::WithOperator)new");
        // Unresolvable, uncallable or non-converting converters, and open target types, are skipped.
        await Assert.That(generated).DoesNotContain("(global::WrongAssembly)new");
        await Assert.That(generated).DoesNotContain("(global::GenericByName)new");
        await Assert.That(generated).DoesNotContain("(global::NoConvertFrom)new");
        await Assert.That(generated).DoesNotContain("(global::UnusableConstructors)new");
        await Assert.That(generated).DoesNotContain("global::Box<");
    }

#if NET
    [Test]
    public async Task ParseTakesPrecedenceOverTypeConverter()
    {
        const string source = """
            using System;
            using System.ComponentModel;
            using System.Globalization;
            using TUnit.Core;
            [TypeConverter(typeof(PlainConverter))]
            public readonly struct Parsable : IParsable<Parsable>
            {
                public static Parsable Parse(string s, IFormatProvider? provider) => default;
                public static bool TryParse(string? s, IFormatProvider? provider, out Parsable result) { result = default; return true; }
            }
            public class PlainConverter : TypeConverter
            {
                public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) => null;
            }
            public class Tests
            {
                [Test, Arguments("a")]
                public void Convert(Parsable parsable) { }
            }
            """;
        var generated = await GenerateTypeConverters(source);
        await Assert.That(generated).DoesNotContain("(global::Parsable)new");
    }

    [Test]
    public async Task SkipsTypeConverterConstructorsLeavingRequiredMembersUnset()
    {
        const string source = """
            using System;
            using System.ComponentModel;
            using System.Diagnostics.CodeAnalysis;
            using System.Globalization;
            using TUnit.Core;
            public abstract class BaseConverter : TypeConverter
            {
                public required string Name { get; set; }
                public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) => null;
            }
            [TypeConverter(typeof(UnsetConverter))]
            public class Unset { }
            [TypeConverter(typeof(SetConverter))]
            public class Set { }
            public class UnsetConverter : BaseConverter { }
            public class SetConverter : BaseConverter
            {
                [SetsRequiredMembers] public SetConverter() => Name = "";
            }
            public class Tests
            {
                [Test]
                public void Convert(Unset unset, Set set) { }
            }
            """;
        var generated = await GenerateTypeConverters(source);
        await Assert.That(generated).DoesNotContain("(global::Unset)new");
        await Assert.That(generated).Contains("(global::Set)new global::SetConverter().ConvertFrom(");
    }
#endif

    private static async Task<string> GenerateTypeConverters(string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("TypeConverters",
            [CSharpSyntaxTree.ParseText(source, parseOptions)], ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new AotConverterGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        await Assert.That(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        return string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(t => t.ToString()));
    }

    [Test]
    public async Task HandlesInheritedPropertyAndAmbiguousSources()
    {
        const string providerSource = """
            public struct HiddenRow { public static implicit operator int(HiddenRow value) => 44; }
            public struct ClassRow { public static implicit operator int(ClassRow value) => 42; }
            public class Provider
            {
                protected class HiddenRows : System.Collections.Generic.List<HiddenRow> { }
                protected static HiddenRows Hidden() => new();
                public static ClassRow ClassData() => new();
            }
            """;
        const string source = """
            using TUnit.Core;
            public struct PropertyRow { public static implicit operator int(PropertyRow value) => 43; }
            public struct AmbiguousRow { public static implicit operator Target(AmbiguousRow value) => new(); }
            public struct Target { public static implicit operator Target(AmbiguousRow value) => new(); }
            public struct ExplicitRow { public static explicit operator ExplicitTarget(ExplicitRow value) => new(); }
            public struct ExplicitTarget { public static explicit operator ExplicitTarget(ExplicitRow value) => new(); }
            public struct LocalRow { public static implicit operator int(LocalRow value) => 45; }
            [MethodDataSource(typeof(Provider), nameof(Provider.ClassData))]
            public class Base
            {
                [MethodDataSource("PropertyData")]
                public int Value { get; set; }
            }
            public class Tests : Base
            {
                public Tests(int value) { }
                public static PropertyRow PropertyData() => new();
                public static AmbiguousRow Rows() => new();
                public static ExplicitRow ExplicitRows() => new();
                [Test, MethodDataSource(nameof(Rows))]
                public void Ambiguous(object value) { }
                [Test, MethodDataSource(nameof(ExplicitRows))]
                public void Explicit(object value) { }
                [Test, MethodDataSource(typeof(Provider), "Hidden")]
                public void Hidden(int value) { }
                [MethodDataSource(nameof(LocalData))]
                public int Local { get; set; }
                public static LocalRow LocalData() => new();
            }
            """;
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var provider = CSharpCompilation.Create("ExternalProvider",
            [CSharpSyntaxTree.ParseText(providerSource, parseOptions)], ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var image = new MemoryStream();
        await Assert.That(provider.Emit(image).Success).IsTrue();
        var compilation = CSharpCompilation.Create("InheritedAndAmbiguousSources",
            [CSharpSyntaxTree.ParseText(source, parseOptions)],
            ReferencesHelper.References.Append(MetadataReference.CreateFromImage(image.ToArray())),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new AotConverterGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        await Assert.That(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        var generated = string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(t => t.ToString()));
        foreach (var type in new[] { "ClassRow", "PropertyRow", "LocalRow", "HiddenRow" })
        {
            await Assert.That(generated).Contains("typeof(global::" + type + ")");
        }
        await Assert.That(generated).DoesNotContain("AmbiguousRow");
        await Assert.That(generated).DoesNotContain("ExplicitRow");
        await Assert.That(generated).DoesNotContain("HiddenRows");
    }

    [Test]
    public async Task FindsMethodSourceWhenMetadataNamesAreAmbiguous()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var duplicate = CSharpCompilation.Create("DuplicateAttributes",
            [CSharpSyntaxTree.ParseText("""
                namespace TUnit.Core
                {
                    public class MethodDataSourceAttribute : System.Attribute { }
                    public class MethodDataSourceAttribute<T> : System.Attribute { }
                }
                """, parseOptions)], ReferencesHelper.References, options);
        using var image = new MemoryStream();
        var emit = duplicate.Emit(image);
        await Assert.That(emit.Success).IsTrue();

        var references = ReferencesHelper.References.Select(reference =>
            Path.GetFileName(reference.FilePath) == "TUnit.Core.dll"
                ? reference.WithAliases(["global", "core"])
                : reference).Append(MetadataReference.CreateFromImage(image.ToArray()));
        var compilation = CSharpCompilation.Create("AmbiguousMethodSources",
            [CSharpSyntaxTree.ParseText("""
                extern alias core;
                using TUnit.Core;
                public struct ScalarValue { public static implicit operator int(ScalarValue value) => 42; }
                public struct GenericValue { public static implicit operator int(GenericValue value) => 43; }
                public class Tests
                {
                    public static ScalarValue Rows() => new();
                    public static GenericValue GenericRows() => new();
                    [Test, core::TUnit.Core.MethodDataSource(nameof(Rows))]
                    public void Scalar(int value) { }
                    [Test, core::TUnit.Core.MethodDataSource<Tests>(nameof(GenericRows))]
                    public void Generic(int value) { }
                }
                """, parseOptions)], references, options);
        await Assert.That(compilation.GetTypeByMetadataName("TUnit.Core.MethodDataSourceAttribute")).IsNull();
        await Assert.That(compilation.GetTypeByMetadataName("TUnit.Core.MethodDataSourceAttribute`1")).IsNull();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new AotConverterGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        await Assert.That(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        var generated = string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(t => t.ToString()));
        await Assert.That(generated).Contains("typeof(global::ScalarValue)");
        await Assert.That(generated).Contains("typeof(global::GenericValue)");
    }

    [Test]
    public async Task HandlesConcreteCollectionsAndInaccessibleSourceTypes()
    {
        const string source = """
            using System.Collections.Generic;
            using TUnit.Core;
            internal struct CollectionValue { public static implicit operator int(CollectionValue value) => 42; }
            internal class Values : List<CollectionValue> { }
            internal struct RecursiveValue { public static implicit operator int(RecursiveValue value) => 42; }
            internal class Rows<T> : IEnumerable<Rows<T[]>>
            {
                public IEnumerator<Rows<T[]>> GetEnumerator() => throw new System.NotSupportedException();
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
            }
            public class Box<T> { public static implicit operator int(Box<T> value) => 42; }
            internal class Tests
            {
                private struct PrivateValue { public static implicit operator int(PrivateValue value) => 42; }
                private class Hidden
                {
                    public struct NestedValue { public static implicit operator int(NestedValue value) => 42; }
                }
                private static PrivateValue PrivateRows() => new();
                private static Hidden.NestedValue NestedRows() => new();
                private static Box<PrivateValue> GenericRows() => new();
                private static Values ConcreteRows() => new();
                private static Rows<RecursiveValue> RecursiveRows() => new();
                [Test, MethodDataSource(nameof(RecursiveRows))]
                public void Recursive(object value) { }
                [Test, MethodDataSource(nameof(PrivateRows))]
                public void Private(int value) { }
                [Test, MethodDataSource(nameof(NestedRows))]
                public void Nested(int value) { }
                [Test, MethodDataSource(nameof(GenericRows))]
                public void Generic(int value) { }
                [Test, MethodDataSource(nameof(ConcreteRows))]
                public void Concrete(int value) { }
                [Test, MethodDataSource("Missing")]
                public void Missing(int value) { }
                [Test, MethodDataSource(typeof(List<>), "Missing")]
                public void OpenGeneric(int value) { }
            }
            """;
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create(
            "ConcreteAndPrivateSources",
            [CSharpSyntaxTree.ParseText(source, parseOptions)],
            ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new AotConverterGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        await Assert.That(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        var generated = string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(t => t.ToString()));
        await Assert.That(generated).Contains("typeof(global::CollectionValue)");
        await Assert.That(generated).Contains("typeof(global::RecursiveValue)");
        await Assert.That(generated).DoesNotContain("PrivateValue");
        await Assert.That(generated).DoesNotContain("NestedValue");
    }

    [Test]
    public async Task FindsMethodSourceOnlyConversions()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using TUnit.Core;
            public struct ScalarValue { public static implicit operator int(ScalarValue value) => 42; }
            public struct TupleValue { public static explicit operator (int, int)(TupleValue value) => (42, 43); }
            public struct ArrayValue { public static implicit operator int(ArrayValue value) => 42; }
            public struct AsyncValue { public static explicit operator int(AsyncValue value) => 42; }
            public struct ClassValue { public static implicit operator int(ClassValue value) => 42; }
            public struct ParameterValue { public static implicit operator int(ParameterValue value) => 42; }
            public struct UnusedValue { public static implicit operator int(UnusedValue value) => 42; }
            public class ProviderBase
            {
                public static ScalarValue Rows(int count) => new();
                public static UnusedValue Rows(string count) => new();
            }
            public class Provider : ProviderBase
            {
                public static Task<IEnumerable<(TupleValue, int)>> Tuples() => null;
                public static ArrayValue[] Array => null;
                public static ValueTask<IAsyncEnumerable<AsyncValue>> AsyncRows = default;
                public static ClassValue ClassRows() => new();
                public static ParameterValue ParameterRows() => new();
            }
            [MethodDataSource(typeof(Provider), nameof(Provider.ClassRows))]
            public class Tests
            {
                public Tests(int value) { }
                [Test, MethodDataSource<Provider>(nameof(Provider.Rows), Arguments = new object[] { 1 })]
                public void Scalar(int value) { }
                [Test, MethodDataSource(typeof(Provider), nameof(Provider.Tuples))]
                public void Tuples((int, int) value, int other) { }
                [Test, MethodDataSource(typeof(Provider), nameof(Provider.Array))]
                public void Array(int value) { }
                [Test, MethodDataSource(typeof(Provider), nameof(Provider.AsyncRows))]
                public void Async(int value) { }
                [Test]
                public void Parameter([MethodDataSource(typeof(Provider), nameof(Provider.ParameterRows))] int value) { }
            }
            """;
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create(
            "MethodSourceConverters",
            [CSharpSyntaxTree.ParseText(source, parseOptions)],
            ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new AotConverterGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        await Assert.That(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        var generated = string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(t => t.ToString()));
        foreach (var type in new[] { "ScalarValue", "TupleValue", "ArrayValue", "AsyncValue", "ClassValue", "ParameterValue" })
        {
            await Assert.That(generated).Contains("typeof(global::" + type + ")");
        }
        await Assert.That(generated).DoesNotContain("global::UnusedValue");
        await Verify(generated);
    }

    [Test]
    public async Task FindsConversionsInNestedAndPartialDeclarations()
    {
        const string source = """
            using TUnit.Core;
            public struct MethodValue { public static implicit operator MethodValue(int value) => new(); }
            public struct ConstructorValue { public static implicit operator ConstructorValue(int value) => new(); }
            public struct NestedValue { public static implicit operator NestedValue(int value) => new(); }
            public struct PartialValue { public static implicit operator PartialValue(int value) => new(); }
            public struct BodyOnlyValue { public static implicit operator BodyOnlyValue(int value) => new(); }
            public partial class Tests
            {
                [Test]
                public partial void Partial(System.Type value);

                [Test, Arguments(1)]
                public void Test(MethodValue value)
                {
                    BodyOnlyValue local = 1;
                    void Helper() { BodyOnlyValue inner = 2; }
                    Helper();
                }
                public class Nested
                {
                    [Test, Arguments(1)]
                    public void Test(NestedValue value) { }
                }
            }
            """;
        const string secondPart = """
            using TUnit.Core;
            [Arguments(1)]
            public partial class Tests
            {
                public partial void Partial([Arguments(typeof(PartialValue))] System.Type value) { }
                public Tests(ConstructorValue value) { BodyOnlyValue local = 1; }
                public BodyOnlyValue Property => 1;
                private BodyOnlyValue field = 1;
            }
            """;

        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create(
            "ConverterDeclarations",
            [CSharpSyntaxTree.ParseText(source, parseOptions), CSharpSyntaxTree.ParseText(secondPart, parseOptions)],
            ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new AotConverterGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        await Assert.That(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();

        var generated = string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(t => t.ToString()));
        await Assert.That(generated).Contains("global::MethodValue");
        await Assert.That(generated).Contains("global::ConstructorValue");
        await Assert.That(generated).Contains("global::NestedValue");
        await Assert.That(generated).Contains("global::PartialValue");
        await Assert.That(generated).DoesNotContain("global::BodyOnlyValue");
    }

    [Test]
    public async Task SkipsConversionsBlockedByObsoleteError()
    {
        // https://github.com/thomhurst/TUnit/issues/7316
        const string source = """
            using System;
            using TUnit.Core;

            // A plain struct, not a positional record: records need IsExternalInit, which net472 lacks.
            public readonly struct Token
            {
                public Token(string value) => Value = value;

                public string Value { get; }

                public static implicit operator Token(string value) => new(value);

                [Obsolete("Use a string.", error: true)]
                public static implicit operator Token(char value) => new(value.ToString());

                [Obsolete("Prefer the string conversion.")]
                public static explicit operator Token(int value) => new(value.ToString());
            }

            public class Tests
            {
                [Test, Arguments("abc")]
                public void Scalar(Token token) { }

                public static (Token Token, int Length) Data() => ("abc", 3);

                [Test, MethodDataSource(nameof(Data))]
                public void Tuple((Token Token, int Length) value) { }
            }
            """;

        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create(
            "ObsoleteConverters",
            [
                CSharpSyntaxTree.ParseText(source, parseOptions),
#if NETFRAMEWORK
                // The record struct needs IsExternalInit, which .NET Framework lacks.
                CSharpSyntaxTree.ParseText(ReferencesHelper.NetFrameworkPolyfills, parseOptions),
#endif
            ],
            ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new AotConverterGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        await Assert.That(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();

        var generated = string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(t => t.ToString()));
        await Assert.That(generated).Contains("typeof(string)");
        await Assert.That(generated).Contains("typeof(int)");
        await Assert.That(generated).DoesNotContain("typeof(char)");
    }
}
