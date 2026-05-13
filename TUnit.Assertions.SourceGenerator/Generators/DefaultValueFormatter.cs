using Microsoft.CodeAnalysis;

namespace TUnit.Assertions.SourceGenerator.Generators;

/// <summary>
/// Shared default-value emit for both assertion source generators. Renders a parameter's
/// Roslyn-reported default value as the C# literal that goes after <c>=</c> in the generated
/// method signature.
/// </summary>
internal static class DefaultValueFormatter
{
    /// <summary>
    /// Formats <paramref name="defaultValue"/> as a C# default-literal expression for a
    /// parameter of <paramref name="type"/>.
    /// </summary>
    /// <param name="defaultValue">The constant value Roslyn reported for the parameter's
    /// explicit default, or <see langword="null"/> if the parameter was declared with the
    /// <c>default</c> keyword (which Roslyn surfaces as a null constant).</param>
    /// <param name="type">The parameter's declared type.</param>
    /// <param name="useFullyQualifiedEnumName">
    /// When <see langword="true"/>, enum members are emitted as <c>Namespace.Enum.Member</c>
    /// (used by <c>MethodAssertionGenerator</c>, which emits fully-qualified type names in
    /// the generated extension). When <see langword="false"/>, enum members are emitted as
    /// <c>Enum.Member</c> without the namespace prefix (used by
    /// <c>AssertionExtensionGenerator</c>, which relies on <c>using</c> directives at the
    /// top of the generated file to resolve the short name).
    /// </param>
    public static string FormatDefaultValue(object? defaultValue, ITypeSymbol type, bool useFullyQualifiedEnumName)
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
