namespace TUnit.Engine;

internal static class PolyfillExtensions
{
    internal static string ReplaceLineEndings(this string value, string replacement)
    {
        return value.Replace("\r\n", replacement)
            .Replace("\n", replacement)
            .Replace("\r", replacement);
    }
}
