using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TUnit.Mock.SourceGenerator.Models;

namespace TUnit.Mock.SourceGenerator.Extensions;

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

    public static string GetGenericConstraints(this ITypeParameterSymbol typeParam)
    {
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

        return constraints.Count > 0 ? string.Join(", ", constraints) : "";
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
            return $"{direction}{p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {p.Name}";
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
