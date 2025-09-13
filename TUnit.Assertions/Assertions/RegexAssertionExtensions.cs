using System.Text.RegularExpressions;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

// Regex pattern validation
[CreateAssertion<Regex>( typeof(RegexAssertionExtensions), nameof(HasTimeout))]
[CreateAssertion<Regex>( typeof(RegexAssertionExtensions), nameof(HasTimeout), CustomName = "HasNoTimeout", NegateLogic = true)]

[CreateAssertion<Regex>( typeof(RegexAssertionExtensions), nameof(IsCaseInsensitive))]
[CreateAssertion<Regex>( typeof(RegexAssertionExtensions), nameof(IsCaseInsensitive), CustomName = "IsCaseSensitive", NegateLogic = true)]

[CreateAssertion<Regex>( typeof(RegexAssertionExtensions), nameof(IsMultiline))]
[CreateAssertion<Regex>( typeof(RegexAssertionExtensions), nameof(IsMultiline), CustomName = "IsSingleline", NegateLogic = true)]

[CreateAssertion<Regex>( typeof(RegexAssertionExtensions), nameof(IsCompiled))]
[CreateAssertion<Regex>( typeof(RegexAssertionExtensions), nameof(IsCompiled), CustomName = "IsNotCompiled", NegateLogic = true)]
public static partial class RegexAssertionExtensions
{
    internal static bool HasTimeout(Regex regex) => regex.MatchTimeout != Regex.InfiniteMatchTimeout;
    internal static bool IsCaseInsensitive(Regex regex) => (regex.Options & RegexOptions.IgnoreCase) != 0;
    internal static bool IsMultiline(Regex regex) => (regex.Options & RegexOptions.Multiline) != 0;
    internal static bool IsCompiled(Regex regex) => (regex.Options & RegexOptions.Compiled) != 0;
}