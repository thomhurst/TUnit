using System.Collections.Concurrent;
using System.Text;

namespace TUnit.Core.Helpers;

/// <summary>
/// Formats <see cref="Type"/> instances into simple, human-readable names for display purposes.
/// </summary>
internal static class TypeNameFormatter
{
    /// <summary>
    /// Builds a simple type name, recursively expanding generic arguments
    /// (e.g. <c>Dictionary&lt;String, List&lt;Int32&gt;&gt;</c>).
    /// </summary>
    /// <remarks>
    /// Uses a single pooled <see cref="StringBuilder"/> for the entire recursive build,
    /// allocating only the final string rather than one string per generic level.
    /// </remarks>
    public static string GetSimpleTypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var builder = StringBuilderPool.Get();
        try
        {
            AppendSimpleTypeName(builder, type);
            return builder.ToString();
        }
        finally
        {
            StringBuilderPool.Return(builder);
        }
    }

    private static void AppendSimpleTypeName(StringBuilder builder, Type type)
    {
        if (!type.IsGenericType)
        {
            builder.Append(type.Name);
            return;
        }

        var genericTypeName = type.GetGenericTypeDefinition().Name;
        var index = genericTypeName.IndexOf('`');
        if (index > 0)
        {
            builder.Append(genericTypeName, 0, index);
        }
        else
        {
            builder.Append(genericTypeName);
        }

        builder.Append('<');

        var genericArgs = type.GetGenericArguments();
        for (var i = 0; i < genericArgs.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            AppendSimpleTypeName(builder, genericArgs[i]);
        }

        builder.Append('>');
    }

    private static readonly ConcurrentDictionary<Type, string> CSharpTypeNameCache = new();

    /// <summary>
    /// Returns the type's name without the CLR generic arity suffix (<c>MyClass`1</c> becomes
    /// <c>MyClass</c>). This is the name the source generator emits (Roslyn's <c>ISymbol.Name</c>),
    /// so reflection mode uses it wherever a name must match source-generated metadata.
    /// </summary>
    public static string GetNameWithoutGenericArity(Type type)
    {
        var name = type.Name;
        var index = name.IndexOf('`');
        return index > 0 ? name.Substring(0, index) : name;
    }

    /// <summary>
    /// Appends <c>&lt;T1, T2&gt;</c> to <paramref name="name"/>, formatting each type argument with
    /// <see cref="GetCSharpTypeName"/>. Used to build the test name of a generic-method
    /// instantiation, for example <c>Method&lt;int, string&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Must stay in step with <c>TypeSymbolExtensions.ToTestNameTypeArgument</c> in
    /// TUnit.Core.SourceGenerator, which formats the same names at compile time.
    /// </remarks>
    public static string AppendGenericArguments(string name, Type[] typeArguments)
    {
        if (typeArguments.Length == 0)
        {
            return name;
        }

        var builder = StringBuilderPool.Get();
        try
        {
            builder.Append(name);
            builder.Append('<');
            for (var i = 0; i < typeArguments.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(GetCSharpTypeName(typeArguments[i]));
            }

            builder.Append('>');
            return builder.ToString();
        }
        finally
        {
            StringBuilderPool.Return(builder);
        }
    }

    /// <summary>
    /// Formats a type the way it reads in C# source, without namespaces: keyword aliases for
    /// built-in types (<c>int</c>, <c>string</c>), <c>T?</c> for nullable value types,
    /// <c>(T1, T2)</c> for value tuples, <c>T[]</c> for arrays, <c>List&lt;int&gt;</c> for
    /// generics and <c>Outer.Inner</c> for nested types. Results are cached per type.
    /// </summary>
    public static string GetCSharpTypeName(Type type)
    {
        return CSharpTypeNameCache.GetOrAdd(type, static t =>
        {
            var builder = StringBuilderPool.Get();
            try
            {
                AppendCSharpTypeName(builder, t);
                return builder.ToString();
            }
            finally
            {
                StringBuilderPool.Return(builder);
            }
        });
    }

    private static void AppendCSharpTypeName(StringBuilder builder, Type type)
    {
        if (type.IsGenericParameter)
        {
            builder.Append(type.Name);
            return;
        }

        if (type.IsArray)
        {
            AppendCSharpTypeName(builder, type.GetElementType()!);
            builder.Append('[');
            builder.Append(',', type.GetArrayRank() - 1);
            builder.Append(']');
            return;
        }

        if (type.HasElementType)
        {
            AppendCSharpTypeName(builder, type.GetElementType()!);
            builder.Append(type.IsPointer ? '*' : '&');
            return;
        }

        if (GetKeyword(type) is { } keyword)
        {
            builder.Append(keyword);
            return;
        }

        var genericArguments = type.IsGenericType ? type.GetGenericArguments() : Type.EmptyTypes;

        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            var definition = type.GetGenericTypeDefinition();

            if (definition == typeof(Nullable<>))
            {
                AppendCSharpTypeName(builder, genericArguments[0]);
                builder.Append('?');
                return;
            }

            if (IsTupleSyntaxDefinition(definition))
            {
                builder.Append('(');
                for (var i = 0; i < genericArguments.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    AppendCSharpTypeName(builder, genericArguments[i]);
                }

                builder.Append(')');
                return;
            }
        }

        var argumentIndex = 0;
        AppendNamedType(builder, type, genericArguments, ref argumentIndex);
    }

    // Writes Outer<A>.Inner<B>. For nested generics the CLR stores every type argument of the
    // whole chain on the innermost type, and each level's own arity is its backtick suffix.
    private static void AppendNamedType(StringBuilder builder, Type type, Type[] genericArguments, ref int argumentIndex)
    {
        if (type.DeclaringType is { } declaringType)
        {
            AppendNamedType(builder, declaringType, genericArguments, ref argumentIndex);
            builder.Append('.');
        }

        var name = type.Name;
        var tick = name.IndexOf('`');
        if (tick <= 0)
        {
            builder.Append(name);
            return;
        }

        builder.Append(name, 0, tick);

#if NET
        var arity = int.Parse(name.AsSpan(tick + 1), System.Globalization.CultureInfo.InvariantCulture);
#else
        var arity = int.Parse(name.Substring(tick + 1), System.Globalization.CultureInfo.InvariantCulture);
#endif
        builder.Append('<');
        for (var i = 0; i < arity && argumentIndex < genericArguments.Length; i++, argumentIndex++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            AppendCSharpTypeName(builder, genericArguments[argumentIndex]);
        }

        builder.Append('>');
    }

    // C# tuple syntax needs at least two elements, and ValueTuple`8 nests its tail in TRest.
    private static bool IsTupleSyntaxDefinition(Type definition)
    {
        return definition == typeof(ValueTuple<,>)
            || definition == typeof(ValueTuple<,,>)
            || definition == typeof(ValueTuple<,,,>)
            || definition == typeof(ValueTuple<,,,,>)
            || definition == typeof(ValueTuple<,,,,,>)
            || definition == typeof(ValueTuple<,,,,,,>);
    }

    private static string? GetKeyword(Type type)
    {
        if (type.IsEnum)
        {
            return null;
        }

        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => "bool",
            TypeCode.Byte => "byte",
            TypeCode.SByte => "sbyte",
            TypeCode.Char => "char",
            TypeCode.Decimal => "decimal",
            TypeCode.Double => "double",
            TypeCode.Single => "float",
            TypeCode.Int16 => "short",
            TypeCode.UInt16 => "ushort",
            TypeCode.Int32 => "int",
            TypeCode.UInt32 => "uint",
            TypeCode.Int64 => "long",
            TypeCode.UInt64 => "ulong",
            TypeCode.String => "string",
            _ when type == typeof(object) => "object",
            _ when type == typeof(void) => "void",
            _ when type == typeof(IntPtr) => "nint",
            _ when type == typeof(UIntPtr) => "nuint",
            _ => null,
        };
    }
}
