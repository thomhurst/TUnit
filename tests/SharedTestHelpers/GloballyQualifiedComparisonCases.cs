using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TUnit.Tests.Shared;

/// <summary>
/// Shared by the TUnit.Analyzers and TUnit.Assertions.Analyzers tests: checks that the
/// name-prefiltered IsGloballyQualified helpers agree with plain display-string comparison for a wide range of symbols.
/// </summary>
public static class GloballyQualifiedComparisonCases
{
    private const string Source = """
        namespace Ns.Inner
        {
            public class Plain
            {
                public void M() { }
                public void G<T>() { }
                public int this[int i] => i;
                public int P { get; set; }
                public static Plain operator +(Plain a, Plain b) => a;
                public static implicit operator int(Plain p) => 0;
                ~Plain() { }
            }

            public class Generic<T>
            {
                public class Nested { public void M() { } }
                public class NestedGeneric<U> { public void M<V>() { } }
            }

            public interface IFoo { void M(); }
            public class Impl : IFoo { void IFoo.M() { } }
            public class Console { public static void WriteLine() { } }
            public class ClassDataSourceAttribute<T> : System.Attribute { }
            public class TimeoutAttribute : System.Attribute { }
            public struct S { }
            public delegate void D();
            public enum E { A }
        }

        namespace Xunit
        {
            public static class Assert
            {
                public static void Equal(int a, int b) { }
                public class Nested { public static void X() { } }
            }
        }

        namespace Xunit.Assert2 { public class Assert { } }

        namespace @class { public class @int { } }

        public class GlobalType { public class Xunit { public class Assert { public static void Equal() { } } } }

        public class _Underscore1 { }

        public static class Extensions { public static void Ext(this Ns.Inner.Plain p) { } }
        """;

    private static readonly string[] ExtraCandidates =
    [
        "", "global::", "global::.", "global::Ns.Inner.Generic<", "global::Ns.Inner.Generic<T>>", "global::Ns.Inner.Generic<T>.",
        "int", "object", "string", "dynamic", "nint", "global::System.Int32", "global::System.Object", "global::System.String",
        "global::System.IntPtr", "global::System.Nullable", "global::System.Nullable<T>", "global::System.ValueTuple",
        "global::Ns.Inner.Plain[]", "global::Ns.Inner.Plain?", "global::Ns.Inner.Plain*", "global::Ns.Inner.Plain.M()",
        "global::Ns.Inner.Generic<T>.Nested", "global::Ns.Inner.Generic.Nested", "global::Ns.Inner.Generic<int>.Nested",
        "global::Xunit.Assert", "global::Xunit.Assert.Equal", "global::GlobalType.Xunit.Assert.Equal",
        "global::System.Threading.CancellationToken", "global::System.IAsyncDisposable", "global::System.Console",
        "global::TUnit.Core.TestAttribute", "global::TUnit.Core.SingleTUnitAttribute", "global::TUnit.Core.DependsOnAttribute",
        "global::TUnit.Core.IDataSourceAttribute", "global::TUnit.Core.TimeoutAttribute",
        "global::System.Runtime.CompilerServices.CallerArgumentExpressionAttribute",
        "global::@class.@int", "global::class.int", "global::_Underscore1", "global::Missing.Type", "global::Missing",
    ];

    public static List<string> FindMismatches(
        string tunitCoreLocation,
        Func<ISymbol, string> globallyQualified,
        Func<ISymbol, string, bool> isGloballyQualified,
        Func<ISymbol, string> globallyQualifiedNonGeneric,
        Func<ISymbol, string, bool> isGloballyQualifiedNonGeneric,
        out int comparisons)
    {
        var references = ((string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(p => Path.GetFileName(p).StartsWith("System.", StringComparison.Ordinal)
                        || Path.GetFileName(p) is "netstandard.dll" or "mscorlib.dll")
            .Append(tunitCoreLocation)
            .Distinct()
            .Select(p => MetadataReference.CreateFromFile(p))
            .ToList();

        var compilation = CSharpCompilation.Create("Test",
            [CSharpSyntaxTree.ParseText(Source, new CSharpParseOptions(LanguageVersion.Preview))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable, allowUnsafe: true));

        var symbols = new List<ISymbol>();

        void AddType(INamedTypeSymbol? type)
        {
            if (type is null)
            {
                return;
            }

            symbols.Add(type);
            symbols.AddRange(type.GetMembers());

            foreach (var nested in type.GetTypeMembers())
            {
                AddType(nested);
            }
        }

        foreach (var type in GetAllTypes(compilation.Assembly.GlobalNamespace))
        {
            AddType(type);
        }

        foreach (var metadataName in new[]
                 {
                     "System.Int32", "System.String", "System.Object", "System.IntPtr", "System.Console",
                     "System.IAsyncDisposable", "System.Threading.CancellationToken", "System.Nullable`1",
                     "System.Collections.Generic.List`1", "System.Collections.Generic.Dictionary`2",
                     "System.ValueTuple`2", "System.Attribute",
                     "System.Runtime.CompilerServices.CallerArgumentExpressionAttribute",
                     "System.Runtime.CompilerServices.CallerMemberNameAttribute",
                     "TUnit.Core.TestAttribute", "TUnit.Core.SingleTUnitAttribute", "TUnit.Core.DependsOnAttribute",
                     "TUnit.Core.DependsOnAttribute`1", "TUnit.Core.IDataSourceAttribute", "TUnit.Core.TimeoutAttribute",
                     "TUnit.Core.ClassDataSourceAttribute`1", "TUnit.Core.MatrixAttribute",
                 })
        {
            AddType(compilation.GetTypeByMetadataName(metadataName));
        }

        var intType = compilation.GetSpecialType(SpecialType.System_Int32);
        var stringType = compilation.GetSpecialType(SpecialType.System_String);
        var plain = compilation.GetTypeByMetadataName("Ns.Inner.Plain")!;
        var generic = compilation.GetTypeByMetadataName("Ns.Inner.Generic`1")!;
        var nestedGeneric = generic.Construct(plain).GetTypeMembers("NestedGeneric")[0];

        symbols.AddRange(
        [
            compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(intType),
            compilation.GetTypeByMetadataName("System.Collections.Generic.List`1")!.Construct(plain),
            compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2")!.Construct(stringType, plain),
            compilation.CreateTupleTypeSymbol([intType, stringType]),
            compilation.CreateArrayTypeSymbol(plain),
            compilation.CreatePointerTypeSymbol(intType),
            compilation.CreateNativeIntegerTypeSymbol(signed: true),
            compilation.DynamicType,
            compilation.CreateErrorTypeSymbol(null, "Missing", 0),
            plain.WithNullableAnnotation(NullableAnnotation.Annotated),
            generic.Construct(intType),
            generic.GetTypeMembers("Nested")[0],
            nestedGeneric.Construct(intType),
            compilation.GetTypeByMetadataName("TUnit.Core.ClassDataSourceAttribute`1")!.Construct(plain),
            compilation.GetTypeByMetadataName("TUnit.Core.DependsOnAttribute`1")!.Construct(plain),
            plain.GetMembers("G").OfType<IMethodSymbol>().Single().Construct(intType),
            compilation.GetTypeByMetadataName("Extensions")!.GetMembers("Ext").OfType<IMethodSymbol>().Single()
                .ReduceExtensionMethod(plain)!,
        ]);

        var candidates = symbols
            .SelectMany(s => new[] { globallyQualified(s), globallyQualifiedNonGeneric(s) })
            .Concat(ExtraCandidates)
            .Distinct()
            .ToList();

        var mismatches = new List<string>();
        comparisons = 0;

        foreach (var symbol in symbols)
        {
            var genericName = globallyQualified(symbol);
            var nonGenericName = globallyQualifiedNonGeneric(symbol);

            foreach (var candidate in candidates)
            {
                comparisons++;

                if (isGloballyQualified(symbol, candidate) != (genericName == candidate))
                {
                    mismatches.Add($"IsGloballyQualified({symbol.Kind} '{genericName}', '{candidate}')");
                }

                if (isGloballyQualifiedNonGeneric(symbol, candidate) != (nonGenericName == candidate))
                {
                    mismatches.Add($"IsGloballyQualifiedNonGeneric({symbol.Kind} '{nonGenericName}', '{candidate}')");
                }
            }
        }

        return mismatches;
    }

    private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol @namespace)
    {
        foreach (var type in @namespace.GetTypeMembers())
        {
            yield return type;
        }

        foreach (var child in @namespace.GetNamespaceMembers())
        {
            foreach (var type in GetAllTypes(child))
            {
                yield return type;
            }
        }
    }
}
