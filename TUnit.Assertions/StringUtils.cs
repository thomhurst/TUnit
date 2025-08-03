namespace TUnit.Assertions;

internal static class StringUtils
{
    public static string? StripWhitespace(string? input)
    {
        if (input == null)
        {
            return null;
        }

        return string.Join(string.Empty, input.Where(c => !char.IsWhiteSpace(c)));
    }

    public static string FindClosestSubstring(string? text, string? pattern,
        StringComparison stringComparison,
        bool ignoreWhitespace,
        out int differIndexOnActual,
        out int differIndexOnExpected)
    {
        if (text is not { Length: > 0 } || pattern is not { Length: > 0 })
        {
            differIndexOnActual = -1;
            differIndexOnExpected = -1;
            return string.Empty;
        }

        differIndexOnExpected = 0;
        differIndexOnActual = 0;

        var actualDictionary = text.Select((x, i) => (Char: x, Index: i)).ToDictionary(x => x.Index, x => x.Char);
        var expectedDictionary = pattern.Select((x, i) => (Char: x, Index: i)).ToDictionary(x => x.Index, x => x.Char);

        if (ignoreWhitespace)
        {
            foreach (var (key, _) in actualDictionary.ToList().Where(x => char.IsWhiteSpace(x.Value)))
            {
                actualDictionary.Remove(key);
            }

            foreach (var (key, _) in expectedDictionary.ToList().Where(x => char.IsWhiteSpace(x.Value)))
            {
                expectedDictionary.Remove(key);
            }
        }

        var actualKeys = actualDictionary.Keys.ToList();

        var matchingConsecutiveCount = 0;

        var c = pattern[0];
        var indexes = actualDictionary.Where(tuple => tuple.Value.Equals(c)).Select(x => x.Key).ToArray();

        foreach (var index in indexes)
        {
            var consecutiveCount = 0;

            var actualCharIndex = actualKeys.IndexOf(index);

            for (var i = 0; i < expectedDictionary.Count; i++)
            {
                int? actualKey = actualKeys.Count > actualCharIndex + i ? actualKeys[actualCharIndex + i] : null;

                if (actualKey == null)
                {
                    break;
                }

                var actualChar = actualDictionary[actualKey.Value];

                var expectedChar = expectedDictionary.Values.ElementAt(i);

                if (string.Equals(actualChar.ToString(), expectedChar.ToString(), stringComparison))
                {
                    consecutiveCount++;

                    if (consecutiveCount > matchingConsecutiveCount)
                    {
                        differIndexOnExpected = expectedDictionary.Keys.ElementAt(i);
                        differIndexOnActual = index + expectedDictionary.Keys.ElementAt(i) - expectedDictionary.Keys.ElementAt(0);
                        matchingConsecutiveCount = consecutiveCount;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        var startIndex = Math.Max(differIndexOnActual - 25, 0);

        return text.Substring(startIndex, Math.Min(text.Length - startIndex, 50));
    }
}
