using System.Collections.Concurrent;
using System.Text;
using TUnit.Core.Helpers;

namespace TUnit.Engine.Helpers;

/// <summary>
/// Formats <see cref="Type"/> instances as ECMA-335 metadata-format full names, as required by
/// <c>TestMethodIdentifierProperty</c> in Microsoft.Testing.Platform. This matches the managed
/// name format (vstest RFC 0017) that platform consumers parse: constructed generics as
/// <c>List`1&lt;System.String&gt;</c>, generic method parameters as <c>!!0</c>, generic type
/// parameters as <c>!0</c>, and nested types separated by <c>+</c>.
/// </summary>
internal static class MetadataTypeNameFormatter
{
    private static readonly ConcurrentDictionary<Type, string> Cache = new();

    public static string GetMetadataFullName(Type type)
    {
        return Cache.GetOrAdd(type, static t =>
        {
            var builder = StringBuilderPool.Get();
            try
            {
                AppendMetadataName(builder, t);
                return builder.ToString();
            }
            finally
            {
                StringBuilderPool.Return(builder);
            }
        });
    }

    private static void AppendMetadataName(StringBuilder builder, Type type)
    {
        if (type.IsGenericParameter)
        {
            builder.Append(type.DeclaringMethod is null ? "!" : "!!");
            builder.Append(type.GenericParameterPosition);
            return;
        }

        if (type.HasElementType)
        {
            AppendMetadataName(builder, type.GetElementType()!);

            if (type.IsArray)
            {
                builder.Append('[');
                builder.Append(',', type.GetArrayRank() - 1);
                builder.Append(']');
            }
            else if (type.IsPointer)
            {
                builder.Append('*');
            }
            else if (type.IsByRef)
            {
                builder.Append('&');
            }

            return;
        }

        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            AppendMetadataName(builder, type.GetGenericTypeDefinition());

            builder.Append('<');
            var genericArguments = type.GetGenericArguments();
            for (var i = 0; i < genericArguments.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                AppendMetadataName(builder, genericArguments[i]);
            }
            builder.Append('>');
            return;
        }

        // Non-generic types and generic type definitions: FullName is already metadata format
        // (namespace-qualified, '+' for nested types, backtick arity for generics).
        builder.Append(type.FullName ?? type.Name);
    }
}
