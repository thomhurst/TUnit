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
    /// Names that don't match any rule are returned unchanged — this is the intended forward-compat
    /// behaviour for unrecognised naming conventions; callers can layer <c>[ShouldName]</c> overrides
    /// on top to rename specific assertions.
    /// </summary>
    public static string Conjugate(string methodName)
    {
        if (string.IsNullOrEmpty(methodName))
        {
            return methodName;
        }

        // Order: longest-prefix-first so IsNot/DoesNot win over Is/Does.
        if (TryReplacePrefix(methodName, "IsNot", "NotBe", out var s)) return s;
        if (TryReplacePrefix(methodName, "Is", "Be", out s)) return s;
        if (TryReplacePrefix(methodName, "Has", "Have", out s)) return s;
        if (TryReplacePrefix(methodName, "DoesNot", "Not", out s)) return s;
        if (TryReplacePrefix(methodName, "Does", "", out s)) return s;
        if (TryDropTrailingS(methodName, out s)) return s;

        return methodName;
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
    /// First-word trailing-s drop. <c>Contains</c>→<c>Contain</c>, <c>StartsWith</c>→<c>StartWith</c>.
    /// Recognises English <c>-es</c> third-person endings (<c>Matches</c>→<c>Match</c>,
    /// <c>Washes</c>→<c>Wash</c>, <c>Fixes</c>→<c>Fix</c>, <c>Buzzes</c>→<c>Buzz</c>,
    /// <c>Goes</c>→<c>Go</c>, <c>Passes</c>→<c>Pass</c>) and drops both letters in those cases.
    /// Plain <c>-ss</c> endings stay put so <c>Pass</c> remains <c>Pass</c>.
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

        if (firstWordEnd < 2 || name[firstWordEnd - 1] != 's')
        {
            result = name;
            return false;
        }

        var dropCount = GetTrailingSDropCount(name, firstWordEnd);
        if (dropCount == 0)
        {
            result = name;
            return false;
        }

        result = name.Substring(0, firstWordEnd - dropCount) + name.Substring(firstWordEnd);
        return true;
    }

    /// <summary>
    /// Returns how many trailing letters of the first word should be removed: 2 for an English
    /// <c>-es</c> 3rd-person ending where the stem ends in a sibilant (<c>ch</c>, <c>sh</c>,
    /// <c>ss</c>, <c>x</c>, <c>z</c>) or in <c>o</c>; 1 for a plain <c>-s</c>; 0 to leave the
    /// name unchanged (e.g. plain <c>-ss</c>, which is a stem rather than a verb form).
    /// </summary>
    private static int GetTrailingSDropCount(string name, int firstWordEnd)
    {
        if (firstWordEnd >= 3 && name[firstWordEnd - 2] == 'e')
        {
            var c = name[firstWordEnd - 3];
            if (c == 'x' || c == 'z' || c == 'o')
            {
                return 2;
            }
            if (firstWordEnd >= 4)
            {
                var c2 = name[firstWordEnd - 4];
                if (c == 'h' && (c2 == 'c' || c2 == 's'))
                {
                    return 2; // ches / shes
                }
                if (c == 's' && c2 == 's')
                {
                    return 2; // sses
                }
            }
            return 1; // generic -es (e.g. Writes -> Write)
        }

        if (name[firstWordEnd - 2] == 's')
        {
            return 0; // plain -ss; not a verb form
        }

        return 1;
    }
}
