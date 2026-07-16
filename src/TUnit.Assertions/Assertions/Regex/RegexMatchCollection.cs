using System.Collections;
using System.Text.RegularExpressions;

namespace TUnit.Assertions.Assertions.Regex;

/// <summary>
/// Collection of all regex matches from a string.
/// Mirrors the .NET System.Text.RegularExpressions.MatchCollection type.
/// </summary>
public class RegexMatchCollection : IReadOnlyList<RegexMatch>
{
    private readonly List<RegexMatch> _matches;

    internal RegexMatchCollection(MatchCollection matches)
    {
        if (matches == null)
        {
            throw new ArgumentNullException(nameof(matches));
        }

        _matches = [];
        foreach (Match match in matches)
        {
            if (match.Success)
            {
                _matches.Add(new RegexMatch(match));
            }
        }

        if (_matches.Count == 0)
        {
            throw new InvalidOperationException("No successful matches found");
        }
    }

    /// <summary>
    /// Gets the first match in the collection.
    /// </summary>
    public RegexMatch First => _matches[0];

    /// <summary>
    /// Gets the match at the specified index.
    /// </summary>
    public RegexMatch this[int index] => _matches[index];

    /// <summary>
    /// Gets the number of matches in the collection.
    /// </summary>
    public int Count => _matches.Count;

    /// <summary>
    /// Returns an enumerator that iterates through the matches.
    /// </summary>
    public IEnumerator<RegexMatch> GetEnumerator() => _matches.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
