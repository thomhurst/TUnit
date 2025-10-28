using System.Text.RegularExpressions;

namespace TUnit.Assertions.Assertions.Regex;

/// <summary>
/// Result of a regex match that provides access to capture groups.
/// </summary>
public class RegexMatchResult
{
    private readonly Match _match;

    internal RegexMatchResult(Match match)
    {
        _match = match ?? throw new ArgumentNullException(nameof(match));
    }

    /// <summary>
    /// Gets the captured string for a named group.
    /// </summary>
    public string Group(string groupName)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            throw new ArgumentException("Group name cannot be null or empty", nameof(groupName));
        }

        return _match.Groups[groupName].Value;
    }

    /// <summary>
    /// Gets the captured string for an indexed group (0 is full match, 1+ are capture groups).
    /// </summary>
    public string Group(int groupIndex)
    {
        if (groupIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(groupIndex), "Group index must be >= 0");
        }

        if (groupIndex >= _match.Groups.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(groupIndex), $"Group index {groupIndex} is out of range. Match has {_match.Groups.Count} groups.");
        }

        return _match.Groups[groupIndex].Value;
    }

    /// <summary>
    /// Gets the index where the match starts in the input string.
    /// </summary>
    public int Index => _match.Index;

    /// <summary>
    /// Gets the length of the matched string.
    /// </summary>
    public int Length => _match.Length;

    /// <summary>
    /// Gets the matched string value.
    /// </summary>
    public string Value => _match.Value;
}
