using System.Text.RegularExpressions;

namespace TUnit.Assertions.Assertions.Regex;

/// <summary>
/// Represents a single regex match with access to capture groups, match position, and length.
/// Mirrors the .NET System.Text.RegularExpressions.Match type.
/// </summary>
public class RegexMatch
{
    internal Match InternalMatch { get; }

    internal RegexMatch(Match match)
    {
        InternalMatch = match ?? throw new ArgumentNullException(nameof(match));

        if (!match.Success)
        {
            throw new InvalidOperationException("Cannot create RegexMatch from an unsuccessful match");
        }
    }

    /// <summary>
    /// Gets the captured string for a named group.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the group name is null, empty, or does not exist in the match.</exception>
    public string GetGroup(string groupName)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            throw new ArgumentException("Group name cannot be null or empty", nameof(groupName));
        }

        var group = InternalMatch.Groups[groupName];
        if (!group.Success)
        {
            throw new ArgumentException(
                $"Group '{groupName}' does not exist in the match",
                nameof(groupName));
        }

        return group.Value;
    }

    /// <summary>
    /// Gets the captured string for an indexed group (0 is full match, 1+ are capture groups).
    /// </summary>
    public string GetGroup(int groupIndex)
    {
        if (groupIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(groupIndex), "Group index must be >= 0");
        }

        if (groupIndex >= InternalMatch.Groups.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(groupIndex), $"Group index {groupIndex} is out of range. Match has {InternalMatch.Groups.Count} groups.");
        }

        return InternalMatch.Groups[groupIndex].Value;
    }

    /// <summary>
    /// Gets the index where the match starts in the input string.
    /// </summary>
    public int Index => InternalMatch.Index;

    /// <summary>
    /// Gets the length of the matched string.
    /// </summary>
    public int Length => InternalMatch.Length;

    /// <summary>
    /// Gets the matched string value.
    /// </summary>
    public string Value => InternalMatch.Value;
}
