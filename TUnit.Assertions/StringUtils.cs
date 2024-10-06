namespace TUnit.Assertions;

internal static class StringUtils
{
    public static string? StripWhitespace(string? input)
    {
        if (input == null)
        {
            return null;
        }
        
        return string.Join(string.Empty, input.Where(c=>!char.IsWhiteSpace(c)));
    }
}