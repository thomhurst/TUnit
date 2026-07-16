using TUnit.Core.Helpers;

namespace TUnit.Engine.Helpers;

internal static class TypeNameHelper
{
    /// <summary>
    /// Appends a type's display name to the builder, expanding generic arguments by their
    /// full name to ensure uniqueness (e.g. <c>List&lt;System.Int32&gt;</c>).
    /// </summary>
    internal static void AppendTypeNameWithGenericArgs(ref ValueStringBuilder vsb, Type type)
    {
        if (!type.IsGenericType)
        {
            vsb.Append(type.Name);
            return;
        }

        var name = type.Name;
        var backtickIndex = name.IndexOf('`');
        if (backtickIndex > 0)
        {
            vsb.Append(name.AsSpan(0, backtickIndex));
        }
        else
        {
            vsb.Append(name);
        }

        // Use the full name for generic arguments to ensure uniqueness.
        var genericArgs = type.GetGenericArguments();
        vsb.Append('<');
        for (var i = 0; i < genericArgs.Length; i++)
        {
            if (i > 0)
            {
                vsb.Append(", ");
            }
            vsb.Append(genericArgs[i].FullName ?? genericArgs[i].Name);
        }
        vsb.Append('>');
    }
}
