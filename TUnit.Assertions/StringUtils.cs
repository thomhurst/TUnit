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

    public static int IndexOfDifference(string? value1, string? value2)
    {
        if (value1 == value2)
        {
            return -1;
        }

        if (value1 is null || value2 is null)
        {
            return 0;
        }
        
        return value1
            .Zip(value2, (item, updated) => (Item: item, Updated: updated))
            .Select((pair, index) => (Pair: pair, Index: index))
            .FirstOrDefault(x => x.Pair.Item != x.Pair.Updated)
            .Index;
    }
}