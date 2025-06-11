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

        return $"{type.Name[..type.Name.IndexOf("`", StringComparison.Ordinal)]}<{genericArguments}>";

    }

    public static bool HasAttribute<T>(this MemberInfo member)
    {
        if (typeof(T).IsAssignableTo(typeof(Attribute)))
        {
            return member.IsDefined(typeof(T), true);
        }

        return member.GetCustomAttributes(true)
            .Any(x => x.GetType().IsAssignableTo(typeof(T)));
    }
}
