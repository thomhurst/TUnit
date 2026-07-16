using Microsoft.CodeAnalysis;

namespace TUnit.Assertions.SourceGenerator.Generators;

/// <summary>
/// Shared default-value emit for both assertion source generators. Renders a parameter's
/// Roslyn-reported default value as the C# literal that goes after <c>=</c> in the generated
/// method signature. The two public entry points differ only in how they format enum members:
/// <see cref="FormatDefaultValue"/> emits the short form (<c>Enum.Member</c>), suitable for
/// generators that rely on <c>using</c> directives at the top of the generated file;
/// <see cref="FormatDefaultValueFullyQualified"/> emits the namespace-prefixed form
/// (<c>Namespace.Enum.Member</c>), suitable for generators that emit fully-qualified type
/// names throughout the produced source.
/// </summary>
internal static class DefaultValueFormatter
{
    /// <summary>
    /// Formats <paramref name="defaultValue"/> as a C# default-literal expression for a
    /// parameter of <paramref name="type"/>, emitting enum members in the short form
    /// (<c>Enum.Member</c>) that depends on a <c>using</c> directive to resolve the type name.
    /// </summary>
    /// <param name="defaultValue">The constant value Roslyn reported for the parameter's
    /// explicit default, or <see langword="null"/> if the parameter was declared with the
    /// <c>default</c> keyword (which Roslyn surfaces as a null constant).</param>
    /// <param name="type">The parameter's declared type.</param>
    public static string FormatDefaultValue(object? defaultValue, ITypeSymbol type)
        => FormatDefaultValueCore(defaultValue, type, useFullyQualifiedEnumName: false);

    /// <summary>
    /// Formats <paramref name="defaultValue"/> as a C# default-literal expression for a
    /// parameter of <paramref name="type"/>, emitting enum members in the fully-qualified form
    /// (<c>Namespace.Enum.Member</c>) so the result resolves regardless of <c>using</c>
    /// directives at the top of the generated file.
    /// </summary>
    /// <param name="defaultValue">The constant value Roslyn reported for the parameter's
    /// explicit default, or <see langword="null"/> if the parameter was declared with the
    /// <c>default</c> keyword (which Roslyn surfaces as a null constant).</param>
    /// <param name="type">The parameter's declared type.</param>
    public static string FormatDefaultValueFullyQualified(object? defaultValue, ITypeSymbol type)
        => FormatDefaultValueCore(defaultValue, type, useFullyQualifiedEnumName: true);

    private static string FormatDefaultValueCore(object? defaultValue, ITypeSymbol type, bool useFullyQualifiedEnumName)
    {
        if (defaultValue == null)
        {
            // A null Roslyn-reported default expression on a non-nullable value type means the
            // parameter was declared with `= default` (e.g. `CancellationToken ct = default`).
            // Emitting `= null` for such a parameter produces CS1750 because the literal null
            // cannot convert to the value-type. Emit the `default` literal: the target type is
            // inferred from the parameter, matching the user's original declaration.
            // Nullable<T> stays on the null path: it accepts a literal null default.
            if (type.IsValueType
                && type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
            {
                return "default";
            }
            return "null";
        }

        if (type.TypeKind == TypeKind.Enum && type is INamedTypeSymbol enumType)
        {
            var enumPrefix = useFullyQualifiedEnumName ? enumType.ToDisplayString() : enumType.Name;
            foreach (var member in enumType.GetMembers())
            {
                if (member is IFieldSymbol { HasConstantValue: true } field &&
                    field.ConstantValue != null &&
                    field.ConstantValue.Equals(defaultValue))
                {
                    return $"{enumPrefix}.{field.Name}";
                }
            }

            return $"({enumType.ToDisplayString()})({defaultValue})";
        }

        if (defaultValue is string str)
        {
            return $"\"{str.Replace("\"", "\\\"")}\"";
        }

        if (defaultValue is bool b)
        {
            return b ? "true" : "false";
        }

        if (defaultValue is char c)
        {
            return $"'{c}'";
        }

        return defaultValue.ToString() ?? "null";
    }
}
