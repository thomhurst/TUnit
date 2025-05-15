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
}