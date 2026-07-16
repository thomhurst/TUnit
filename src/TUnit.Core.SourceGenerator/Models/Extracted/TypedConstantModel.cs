using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models.Extracted;

/// <summary>
/// Primitive representation of a TypedConstant (attribute argument value).
/// Contains only strings and primitives - no Roslyn symbols.
/// </summary>
public sealed class TypedConstantModel : IEquatable<TypedConstantModel>
{
    public required string TypeName { get; init; }
    public required string? Value { get; init; }
    public required TypedConstantKind Kind { get; init; }
    public required EquatableArray<TypedConstantModel> ArrayValues { get; init; }
    public required bool IsNull { get; init; }

    public bool Equals(TypedConstantModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return TypeName == other.TypeName
               && Value == other.Value
               && Kind == other.Kind
               && IsNull == other.IsNull
               && ArrayValues.Equals(other.ArrayValues);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TypedConstantModel);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = TypeName.GetHashCode();
            hash = (hash * 397) ^ (Value?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ (int)Kind;
            hash = (hash * 397) ^ IsNull.GetHashCode();
            hash = (hash * 397) ^ ArrayValues.GetHashCode();
            return hash;
        }
    }

    /// <summary>
    /// Extract a TypedConstantModel from a Roslyn TypedConstant.
    /// All symbol access happens here - the returned model contains only primitives.
    /// </summary>
    public static TypedConstantModel Extract(TypedConstant constant)
    {
        if (constant.Kind == TypedConstantKind.Array)
        {
            var arrayValues = constant.Values.IsDefault
                ? EquatableArray<TypedConstantModel>.Empty
                : constant.Values.Select(Extract).ToEquatableArray();

            return new TypedConstantModel
            {
                TypeName = constant.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "object",
                Value = null,
                Kind = constant.Kind,
                ArrayValues = arrayValues,
                IsNull = constant.IsNull
            };
        }

        return new TypedConstantModel
        {
            TypeName = constant.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "object",
            Value = FormatValue(constant),
            Kind = constant.Kind,
            ArrayValues = EquatableArray<TypedConstantModel>.Empty,
            IsNull = constant.IsNull
        };
    }

    private static string? FormatValue(TypedConstant constant)
    {
        if (constant.IsNull)
        {
            return null;
        }

        return constant.Kind switch
        {
            TypedConstantKind.Primitive => FormatPrimitiveValue(constant.Value),
            TypedConstantKind.Enum => FormatEnumValue(constant),
            TypedConstantKind.Type => (constant.Value as ITypeSymbol)?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            _ => constant.Value?.ToString()
        };
    }

    private static string? FormatPrimitiveValue(object? value)
    {
        return value switch
        {
            null => null,
            string s => s,
            char c => c.ToString(),
            bool b => b ? "true" : "false",
            _ => value.ToString()
        };
    }

    private static string? FormatEnumValue(TypedConstant constant)
    {
        if (constant.Value is null || constant.Type is null)
        {
            return null;
        }

        // Store as "TypeName.MemberName" format
        var enumType = constant.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var underlyingValue = constant.Value;

        // Try to find the enum member name
        foreach (var member in constant.Type.GetMembers())
        {
            if (member is IFieldSymbol field && field.HasConstantValue && Equals(field.ConstantValue, underlyingValue))
            {
                return $"{enumType}.{field.Name}";
            }
        }

        // Fallback to numeric value with cast
        return $"({enumType}){underlyingValue}";
    }
}
