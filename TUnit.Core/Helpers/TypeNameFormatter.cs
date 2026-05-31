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
}
