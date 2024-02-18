namespace TUnit.Assertions.Extensions;

internal static class StringExtensions
{
    public static string GetStringOrEmpty(this string? value)
    {
        return GetStringOr(value, string.Empty);
    }
    
    public static string GetStringOr(this string? value, string defaultValue)
    {
        return value ?? defaultValue;
    }
}