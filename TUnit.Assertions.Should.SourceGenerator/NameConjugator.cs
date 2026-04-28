namespace TUnit.Assertions.Should.SourceGenerator;

/// <summary>
/// Pure-function transformer from <c>Is*</c>/<c>Has*</c>/<c>Does*</c>/3rd-person-singular method
/// names produced by <c>[AssertionExtension]</c> to FluentAssertions-style imperative
/// Should-flavored equivalents (<c>BeEqualTo</c>, <c>HaveCount</c>, <c>NotContain</c>, etc.).
/// </summary>
internal static class NameConjugator
{
    /// <summary>
    /// Conjugate an <c>[AssertionExtension]</c> method name into its Should-flavored counterpart.
    /// Returns the conjugated name plus a flag indicating whether a known rule was applied.
    /// When <c>Matched</c> is false the original name is returned unchanged so the caller can
    /// emit a TUSHOULD001 diagnostic.
    /// </summary>
    public static (string Name, bool Matched) Conjugate(string methodName)
    {
        if (string.IsNullOrEmpty(methodName))
        {
            return (methodName, false);
        }

        // Order: longest-prefix-first so IsNot/DoesNot win over Is/Does.
        if (TryReplacePrefix(methodName, "IsNot", "NotBe", out var s)) return (s, true);
        if (TryReplacePrefix(methodName, "Is", "Be", out s)) return (s, true);
        if (TryReplacePrefix(methodName, "Has", "Have", out s)) return (s, true);
        if (TryReplacePrefix(methodName, "DoesNot", "Not", out s)) return (s, true);
        if (TryReplacePrefix(methodName, "Does", "", out s)) return (s, true);
        if (TryDropTrailingS(methodName, out s)) return (s, true);

        return (methodName, false);
    }

    /// <summary>
    /// Replaces <paramref name="prefix"/> with <paramref name="replacement"/> only when it ends
    /// at a CamelCase word boundary (next char uppercase, or end of name). Prevents matches like
    /// "Issue" → "Besue".
    /// </summary>
    private static bool TryReplacePrefix(string name, string prefix, string replacement, out string result)
    {
        if (!name.StartsWith(prefix, System.StringComparison.Ordinal)
            || (name.Length > prefix.Length && !char.IsUpper(name[prefix.Length])))
        {
            result = name;
            return false;
        }

        result = replacement + name.Substring(prefix.Length);
        return true;
    }

    /// <summary>
    /// First-word -s drop. <c>Contains</c>→<c>Contain</c>, <c>StartsWith</c>→<c>StartWith</c>.
    /// Skips <c>-ss</c> endings so <c>Pass</c> stays <c>Pass</c>.
    /// </summary>
    private static bool TryDropTrailingS(string name, out string result)
    {
        var firstWordEnd = name.Length;
        for (var i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]))
            {
                firstWordEnd = i;
                break;
            }
        }

        if (firstWordEnd < 2
            || name[firstWordEnd - 1] != 's'
            || name[firstWordEnd - 2] == 's')
        {
            result = name;
            return false;
        }

        result = name.Substring(0, firstWordEnd - 1) + name.Substring(firstWordEnd);
        return true;
    }
}
