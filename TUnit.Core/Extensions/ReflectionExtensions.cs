using System.Reflection;

namespace TUnit.Core.Extensions;

internal static class ReflectionExtensions
{
    public static string GetFormattedName(this Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var genericArguments = string.Join(",", type.GetGenericArguments().Select(GetFormattedName));

        var backtickIndex = type.Name.IndexOf("`", StringComparison.Ordinal);
        if (backtickIndex == -1)
        {
            return $"{type.Name}<{genericArguments}>";
        }
        
        return $"{type.Name[..backtickIndex]}<{genericArguments}>";

    }

    public static bool HasExactAttribute<T>(this MemberInfo member)
    {
        return HasAttribute<T>(member, false);
    }

    public static bool HasAttribute<T>(this MemberInfo member, bool inherit = true)
    {
        if (typeof(T).IsAssignableTo(typeof(Attribute)))
        {
            return member.IsDefined(typeof(T), inherit);
        }

        return member.GetCustomAttributes(inherit)
            .Any(x => x.GetType().IsAssignableTo(typeof(T)));
    }
}
