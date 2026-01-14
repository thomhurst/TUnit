using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models.Extracted;

/// <summary>
/// Primitive representation of a method parameter.
/// Contains only strings and primitives - no Roslyn symbols.
/// </summary>
public sealed class ParameterModel : IEquatable<ParameterModel>
{
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public required bool HasDefaultValue { get; init; }
    public required string? DefaultValue { get; init; }
    public required bool IsParams { get; init; }
    public required bool IsOut { get; init; }
    public required bool IsRef { get; init; }
    public required EquatableArray<ExtractedAttribute> Attributes { get; init; }

    public bool Equals(ParameterModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name == other.Name
               && TypeName == other.TypeName
               && HasDefaultValue == other.HasDefaultValue
               && DefaultValue == other.DefaultValue
               && IsParams == other.IsParams
               && IsOut == other.IsOut
               && IsRef == other.IsRef
               && Attributes.Equals(other.Attributes);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ParameterModel);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = Name.GetHashCode();
            hash = (hash * 397) ^ TypeName.GetHashCode();
            hash = (hash * 397) ^ HasDefaultValue.GetHashCode();
            hash = (hash * 397) ^ (DefaultValue?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ IsParams.GetHashCode();
            hash = (hash * 397) ^ IsOut.GetHashCode();
            hash = (hash * 397) ^ IsRef.GetHashCode();
            hash = (hash * 397) ^ Attributes.GetHashCode();
            return hash;
        }
    }

    /// <summary>
    /// Extract a ParameterModel from a Roslyn IParameterSymbol.
    /// All symbol access happens here - the returned model contains only primitives.
    /// </summary>
    public static ParameterModel Extract(IParameterSymbol parameter)
    {
        return new ParameterModel
        {
            Name = parameter.Name,
            TypeName = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            HasDefaultValue = parameter.HasExplicitDefaultValue,
            DefaultValue = parameter.HasExplicitDefaultValue ? FormatDefaultValue(parameter) : null,
            IsParams = parameter.IsParams,
            IsOut = parameter.RefKind == RefKind.Out,
            IsRef = parameter.RefKind == RefKind.Ref,
            Attributes = ExtractedAttribute.ExtractAll(parameter)
        };
    }

    /// <summary>
    /// Extract parameters from a method symbol.
    /// </summary>
    public static EquatableArray<ParameterModel> ExtractAll(IMethodSymbol method)
    {
        return method.Parameters
            .Select(Extract)
            .ToEquatableArray();
    }

    private static string? FormatDefaultValue(IParameterSymbol parameter)
    {
        if (!parameter.HasExplicitDefaultValue)
        {
            return null;
        }

        var value = parameter.ExplicitDefaultValue;

        return value switch
        {
            null => "null",
            string s => $"\"{EscapeString(s)}\"",
            char c => $"'{EscapeChar(c)}'",
            bool b => b ? "true" : "false",
            _ when parameter.Type.TypeKind == TypeKind.Enum =>
                $"({parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){value}",
            _ => value.ToString()
        };
    }

    private static string EscapeString(string s)
    {
        return s
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private static string EscapeChar(char c)
    {
        return c switch
        {
            '\\' => "\\\\",
            '\'' => "\\'",
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            _ => c.ToString()
        };
    }
}
