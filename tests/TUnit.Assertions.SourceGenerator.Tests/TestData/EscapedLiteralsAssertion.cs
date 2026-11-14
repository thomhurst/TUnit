#if NET
using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: string values containing quotes and backslashes in forwarded attributes,
/// expectation messages and parameter defaults.
/// Should be emitted as correctly escaped C# literals.
/// </summary>
public static partial class EscapedLiteralsExtensions
{
    [GenerateAssertion(ExpectationMessage = "be \"quoted\" \\t here")]
    [Obsolete("Use \"IsQuotedV2\" \\ instead", DiagnosticId = "ID\\1", UrlFormat = "https://example.com/\"{0}\"")]
    [RequiresUnreferencedCode("Needs \"reflection\" \\ here", Url = "https://example.com/\"ruc\"")]
    [RequiresDynamicCode("Needs \"dynamic\" \\ code", Url = "https://example.com/\\dyn")]
    public static bool IsQuoted(this string value) => value.Contains('"');

    [GenerateAssertion(ExpectationMessage = "contain \"{needle}\" \\ {_needle.Length} chars")]
    public static bool ContainsQuoted(this string value, string needle = "a\"b\\c", char separator = '\\', char quote = '\'')
        => value.Contains(needle);

    [GenerateAssertion(ExpectationMessage = "match \"{_needle.Replace(\"{\", \"(\")}\" {{literal}}")]
    public static bool MatchesBraced(this string value, string needle)
        => value.Contains(needle);
}
#endif
