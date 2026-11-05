namespace TUnit.Tests.Shared;

// One list drives analyzer diagnostics, compiled runtime fixtures, and engine result counts.
// Runtime expectations deliberately differ from analyzer policy for narrowing/explicit conversions.
internal sealed record TupleConversionCase(string Name, string Source, string Target, string Value,
    string Expected, bool Accepted = true, bool Binds = true, bool BindsAot = true, string Registration = "",
    bool? BindsReflection = null);

internal static class TupleConversionMatrix
{
    // Both invokers reconstruct nested tuples; reflection additionally widens char to float.
    // Analyzer acceptance remains conservative for these structural conversions.
    public static readonly TupleConversionCase[] Cases =
    [
        new("Identity", "int", "int", "42", "42"),
        new("Widen", "int", "long", "42", "42L"),
        new("Narrow", "long", "int", "42L", "42", Accepted: false),
        new("NullableWiden", "int?", "long?", "(int?)42", "(long?)42"),
        new("Lift", "int", "long?", "42", "(long?)42"),
        new("Decimal", "int", "decimal", "42", "42m"),
        new("CharacterDecimal", "char", "decimal", "'A'", "65m"),
        new("CharacterFloat", "char", "float", "'A'", "65f", false, false, false, BindsReflection: true),
        new("NullableCharacterDouble", "char?", "double?", "(char?)'A'", "(double?)65", false, false, false),
        new("NativeSigned", "int", "nint", "42", "(nint)42"),
        new("NullableNativeSigned", "int?", "nint?", "(int?)42", "(nint?)42"),
        new("NativeUnsigned", "uint", "nuint", "42U", "(nuint)42"),
        new("NullableNativeUnsigned", "uint?", "nuint?", "(uint?)42", "(nuint?)42"),
        new("SmallNative", "byte", "nint", "(byte)42", "(nint)42", false, false, false),
        new("NativeFloating", "nint", "double", "(nint)42", "42d", false, false, false),
        new("NativeDecimal", "nuint", "decimal", "(nuint)42", "42m", false, false, false),
        new("Reference", "string", "object", "\"value\"", "\"value\""),
        new("Incompatible", "object", "int", "new object()", "42", false, false, false),
        new("NestedValue", "(int, int)", "(long, long)", "(1, 2)", "(1L, 2L)"),
        new("NullableValueIdentity", "(int, int)", "(int, int)?", "(1, 2)", "((int, int)?)(1, 2)"),
        new("NullableValueWiden", "(int, int)", "(long, long)?", "(1, 2)", "((long, long)?)(1L, 2L)", false, true, true, BindsReflection: true),
        new("NestedReferenceIdentity", "Tuple<int>", "Tuple<int>", "Tuple.Create(42)", "Tuple.Create(42)"),
        new("NestedReferenceWiden", "Tuple<int>", "Tuple<long>", "Tuple.Create(42)", "Tuple.Create(42L)", false, true, true, BindsReflection: true),
        // AOT binds source-only operators through converters discovered from the method data
        // source's return type (#7511), so these rows bind in every mode.
        new("ExactOperator", "ImplicitSource", "Converted", "new ImplicitSource()", "new Converted(42)"),
        new("TupleOperator", "TupleSource", "(int, int)", "new TupleSource()", "(1, 2)"),
        new("NullableTupleOperator", "TupleSource", "(int, int)?", "new TupleSource()", "((int, int)?)(1, 2)"),
        new("ExplicitOperator", "ExplicitSource", "Converted", "new ExplicitSource()", "new Converted(42)", Accepted: false),
        new("ComposedOperator", "ImplicitSource", "Base", "new ImplicitSource()", "new Converted(42)", false, false, false),
        // A distinct source type prevents registry/cache state from changing default-case results.
        new("RegisteredOperator", "RegisteredSource", "Converted", "new RegisteredSource()", "new Converted(42)",
            Registration: "TUnit.Core.Converters.AotConverterRegistry.Register<RegisteredSource, Converted>(value => value);"),
    ];

    public const string Declarations = """
        public record Base;
        public record Converted(int Value) : Base;
        public sealed class ImplicitSource { public static implicit operator Converted(ImplicitSource value) => new(42); }
        public sealed class TupleSource { public static implicit operator (int, int)(TupleSource value) => (1, 2); }
        public sealed class ExplicitSource { public static explicit operator Converted(ExplicitSource value) => new(42); }
        public sealed class RegisteredSource { public static implicit operator Converted(RegisteredSource value) => new(42); }
        """;

    public static string Fixture(TupleConversionCase row) => string.Join("\n", ($$"""
        public class {{row.Name}}
        {
            public static Tuple<{{row.Source}}> Row()
            {
                {{row.Registration}}
                return Tuple.Create<{{row.Source}}>({{row.Value}});
            }
            public static IEnumerable<Tuple<{{row.Source}}>> Rows() => [Row()];
            [Test, MethodDataSource(nameof(Row))]
            public void {{row.Name}}_Direct(Tuple<{{row.Target}}> value) => Check(value);
            [Test, MethodDataSource(nameof(Rows))]
            public void {{row.Name}}_Enumerable(Tuple<{{row.Target}}> value) => Check(value);
            private static void Check(Tuple<{{row.Target}}> value)
            {
                if (!Equals(value, Tuple.Create<{{row.Target}}>({{row.Expected}})))
                    throw new InvalidOperationException("Wrong bound value: " + value);
            }
        }
        """).Split('\n').Select(line => line.TrimEnd()));

    public static string RuntimeSource() => """
        // Generated by TupleConversionMatrix.RuntimeSource().
        // Run TupleConversionMatrixTests.RuntimeFixturesStayInSync after changing the matrix;
        // copy the emitted .received.txt to this file after reviewing it.
        using System;
        using System.Collections.Generic;
        using TUnit.Core;
        #pragma warning disable TUnit0001 // Rejected cases deliberately exercise runtime binding.
        namespace TUnit.TestProject.TupleConversions;

        """ + "\n" + Declarations + "\n\n" + string.Join("\n\n", Cases.Select(Fixture)) + "\n";
}
