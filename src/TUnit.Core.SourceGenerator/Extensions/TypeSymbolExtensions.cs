using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Extensions;

internal static class TypeSymbolExtensions
{
    /// <summary>
    /// Determines whether this parameter collects trailing argument values into an array —
    /// either a <c>params T[]</c> parameter or a plain trailing <c>T[]</c> parameter. The
    /// latter lets <c>[Arguments(["a", "b"])]</c> map onto a single array parameter (issue #6120).
    /// </summary>
    /// <remarks>
    /// This is a type-level check only — it does not verify that the parameter is actually the
    /// last one. Callers are responsible for the positional constraint (i.e. only invoking this
    /// on the final parameter), since a non-trailing array binds value-by-value, not collected.
    /// </remarks>
    public static bool CollectsTrailingArguments(this IParameterSymbol parameter)
        => parameter.IsParams || parameter.Type is IArrayTypeSymbol;

    /// <summary>
    /// Determines if the type is a nullable value type (e.g., int?, bool?, MyEnum?)
    /// </summary>
    public static bool IsNullableValueType(this ITypeSymbol typeSymbol)
    {
        return typeSymbol is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T };
    }
    
    /// <summary>
    /// Determines if the type is nullable (either nullable value type or nullable reference type)
    /// </summary>
    public static bool IsNullable(this ITypeSymbol typeSymbol)
    {
        // Check for nullable value types (e.g., int?, bool?)
        if (IsNullableValueType(typeSymbol))
        {
            return true;
        }
        
        // Check for nullable reference types (e.g., string?)
        return typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
    }
    
    /// <summary>
    /// Gets the underlying type of a nullable value type, or null if the type is not nullable
    /// </summary>
    public static ITypeSymbol? GetNullableUnderlyingType(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } namedType)
        {
            return namedType.TypeArguments[0];
        }
        return null;
    }
    /// <summary>
    /// Formats a generic-method type argument for a test name, such as the <c>int</c> in
    /// <c>Method&lt;int&gt;</c>: C# keywords for built-in types, no namespaces, <c>T?</c> for
    /// nullable value types, <c>(T1, T2)</c> for value tuples, <c>T[]</c> for arrays and
    /// <c>Outer.Inner</c> for nested types.
    /// </summary>
    /// <remarks>
    /// Must stay in step with <c>TypeNameFormatter.GetCSharpTypeName</c> in TUnit.Core, which formats
    /// the same names at runtime in reflection mode. Both modes must produce the same name (#7105).
    /// </remarks>
    public static string ToTestNameTypeArgument(this ITypeSymbol type)
    {
        var builder = new System.Text.StringBuilder();
        AppendTestNameTypeArgument(builder, type);
        return builder.ToString();
    }

    private static void AppendTestNameTypeArgument(System.Text.StringBuilder builder, ITypeSymbol type)
    {
        switch (type)
        {
            case ITypeParameterSymbol:
                builder.Append(type.Name);
                return;
            case IDynamicTypeSymbol:
                builder.Append("object");
                return;
            case IArrayTypeSymbol array:
                AppendTestNameTypeArgument(builder, array.ElementType);
                builder.Append('[');
                builder.Append(',', array.Rank - 1);
                builder.Append(']');
                return;
            case IPointerTypeSymbol pointer:
                AppendTestNameTypeArgument(builder, pointer.PointedAtType);
                builder.Append('*');
                return;
        }

        if (GetTestNameKeyword(type.SpecialType) is { } keyword)
        {
            builder.Append(keyword);
            return;
        }

        if (type is not INamedTypeSymbol named)
        {
            builder.Append(type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            return;
        }

        named = named.TupleUnderlyingType ?? named;

        if (named.IsGenericType && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            AppendTestNameTypeArgument(builder, named.TypeArguments[0]);
            builder.Append('?');
            return;
        }

        if (named is { Name: "ValueTuple", Arity: >= 2 and <= 7, ContainingType: null }
            && named.ContainingNamespace is { Name: "System", ContainingNamespace.IsGlobalNamespace: true })
        {
            builder.Append('(');
            AppendTestNameTypeArguments(builder, named.TypeArguments);
            builder.Append(')');
            return;
        }

        AppendTestNameNamedType(builder, named);
    }

    private static void AppendTestNameNamedType(System.Text.StringBuilder builder, INamedTypeSymbol named)
    {
        if (named.ContainingType is { } containingType)
        {
            AppendTestNameNamedType(builder, containingType);
            builder.Append('.');
        }

        builder.Append(named.Name);

        if (named.Arity > 0)
        {
            builder.Append('<');
            AppendTestNameTypeArguments(builder, named.TypeArguments);
            builder.Append('>');
        }
    }

    private static void AppendTestNameTypeArguments(System.Text.StringBuilder builder, IReadOnlyList<ITypeSymbol> typeArguments)
    {
        for (var i = 0; i < typeArguments.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            AppendTestNameTypeArgument(builder, typeArguments[i]);
        }
    }

    private static string? GetTestNameKeyword(SpecialType specialType) => specialType switch
    {
        SpecialType.System_Boolean => "bool",
        SpecialType.System_Byte => "byte",
        SpecialType.System_SByte => "sbyte",
        SpecialType.System_Char => "char",
        SpecialType.System_Decimal => "decimal",
        SpecialType.System_Double => "double",
        SpecialType.System_Single => "float",
        SpecialType.System_Int16 => "short",
        SpecialType.System_UInt16 => "ushort",
        SpecialType.System_Int32 => "int",
        SpecialType.System_UInt32 => "uint",
        SpecialType.System_Int64 => "long",
        SpecialType.System_UInt64 => "ulong",
        SpecialType.System_String => "string",
        SpecialType.System_Object => "object",
        SpecialType.System_Void => "void",
        SpecialType.System_IntPtr => "nint",
        SpecialType.System_UIntPtr => "nuint",
        _ => null,
    };
}