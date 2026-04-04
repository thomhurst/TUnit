using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TUnit.Mocks.SourceGenerator.Models;

namespace TUnit.Mocks.SourceGenerator.Extensions;

internal static class MethodSymbolExtensions
{
    public static ParameterDirection GetParameterDirection(this IParameterSymbol param)
    {
        return param.RefKind switch
        {
            RefKind.Out => ParameterDirection.Out,
            RefKind.Ref => ParameterDirection.Ref,
            RefKind.In => ParameterDirection.In_Readonly,
            _ => ParameterDirection.In
        };
    }

    private static bool IsUnconstrained(this ITypeParameterSymbol typeParam) =>
        !typeParam.HasReferenceTypeConstraint &&
        !typeParam.HasValueTypeConstraint &&
        !typeParam.HasUnmanagedTypeConstraint &&
        !typeParam.HasNotNullConstraint &&
        typeParam.ConstraintTypes.Length == 0 &&
        !typeParam.HasConstructorConstraint;

    public static string GetGenericConstraints(this ITypeParameterSymbol typeParam)
    {
        if (typeParam.IsUnconstrained())
            return "";

        var constraints = new List<string>();

        if (typeParam.HasReferenceTypeConstraint)
            constraints.Add("class");
        if (typeParam.HasValueTypeConstraint)
            constraints.Add("struct");
        if (typeParam.HasUnmanagedTypeConstraint)
            constraints.Add("unmanaged");
        if (typeParam.HasNotNullConstraint)
            constraints.Add("notnull");

        foreach (var constraintType in typeParam.ConstraintTypes)
        {
            constraints.Add(constraintType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        if (typeParam.HasConstructorConstraint)
            constraints.Add("new()");

        return string.Join(", ", constraints);
    }

    public static bool IsUnconstrainedWithNullableUsage(this ITypeParameterSymbol typeParam, IMethodSymbol method)
    {
        if (!typeParam.IsUnconstrained())
        {
            return false;
        }

        if (HasNullableTypeParameter(method.ReturnType, typeParam))
            return true;

        foreach (var param in method.Parameters)
        {
            if (HasNullableTypeParameter(param.Type, typeParam))
                return true;
        }

        return false;
    }

    private static bool HasNullableTypeParameter(ITypeSymbol type, ITypeParameterSymbol typeParam)
    {
        if (type is ITypeParameterSymbol tp &&
            SymbolEqualityComparer.Default.Equals(tp.OriginalDefinition, typeParam.OriginalDefinition) &&
            tp.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return true;
        }

        if (type is INamedTypeSymbol named)
        {
            foreach (var arg in named.TypeArguments)
            {
                if (HasNullableTypeParameter(arg, typeParam))
                    return true;
            }
        }

        if (type is IArrayTypeSymbol array)
        {
            if (HasNullableTypeParameter(array.ElementType, typeParam))
                return true;
        }

        return false;
    }

    public static string GetParameterList(this IMethodSymbol method)
    {
        return string.Join(", ", method.Parameters.Select(p =>
        {
            var direction = p.RefKind switch
            {
                RefKind.Out => "out ",
                RefKind.Ref => "ref ",
                RefKind.In => "in ",
                _ => ""
            };
            return $"{direction}{p.Type.GetFullyQualifiedNameWithNullability()} {p.Name}";
        }));
    }

    public static string GetTypeParameterList(this IMethodSymbol method)
    {
        if (method.TypeParameters.Length == 0) return "";
        return "<" + string.Join(", ", method.TypeParameters.Select(tp => tp.Name)) + ">";
    }

    public static string GetConstraintClauses(this IMethodSymbol method)
    {
        if (method.TypeParameters.Length == 0) return "";

        var sb = new StringBuilder();
        foreach (var tp in method.TypeParameters)
        {
            var constraints = tp.GetGenericConstraints();
            if (!string.IsNullOrEmpty(constraints))
            {
                sb.Append($" where {tp.Name} : {constraints}");
            }
        }
        return sb.ToString();
    }
}
