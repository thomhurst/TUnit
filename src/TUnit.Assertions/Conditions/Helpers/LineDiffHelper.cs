using System.Text;

namespace TUnit.Assertions.Conditions.Helpers;

/// <summary>
/// Helper class for generating line-based diffs for multiline string comparisons.
/// Provides cleaner output than character-based pointer approach for strings with newlines.
/// </summary>
internal static class LineDiffHelper
{
    /// <summary>
    /// Options for controlling diff generation.
    /// </summary>
    public readonly record struct DiffOptions
    {
        /// <summary>
        /// Number of context lines to show before and after differences. Default: 2.
        /// </summary>
        public int ContextLines { get; init; }

        /// <summary>
        /// Maximum number of difference regions to show. Default: 3.
        /// </summary>
        public int MaxDifferences { get; init; }

        /// <summary>
        /// Maximum line length before truncation. Default: 200.
        /// </summary>
        public int MaxLineLength { get; init; }

        /// <summary>
        /// Maximum number of lines to process. Default: 1000.
        /// </summary>
        public int MaxLines { get; init; }

        public static DiffOptions Default => new()
        {
            ContextLines = 2,
            MaxDifferences = 3,
            MaxLineLength = 200,
            MaxLines = 1000
        };
    }

    /// <summary>
    /// Result of a line-based diff operation.
    /// </summary>
    public readonly record struct LineDiffResult
    {
        /// <summary>
        /// Whether the two strings are equal.
        /// </summary>
        public bool AreEqual { get; init; }

        /// <summary>
        /// The 1-based line number where the first difference occurs.
        /// </summary>
        public int FirstDifferentLine { get; init; }

        /// <summary>
        /// The formatted diff output for display in assertion messages.
        /// </summary>
        public string FormattedDiff { get; init; }

        public static LineDiffResult Equal() => new() { AreEqual = true, FormattedDiff = string.Empty };
        public static LineDiffResult Different(int line, string diff) => new() { AreEqual = false, FirstDifferentLine = line, FormattedDiff = diff };
    }

    private enum LineChangeType
    {
        Unchanged,
        Modified,
        Added,
        Removed
    }

    private readonly record struct LineChange(int LineNumber, LineChangeType Type, string? ExpectedLine, string? ActualLine);

    /// <summary>
    /// Determines if line-based diff should be used for the given strings.
    /// Returns true if either string contains newline characters.
    /// </summary>
    public static bool ShouldUseLineDiff(string? actual, string? expected)
    {
        if (actual == null || expected == null)
        {
            return false;
        }

        return ContainsNewline(actual) || ContainsNewline(expected);
    }

    /// <summary>
    /// Generates a line-based diff between the actual and expected strings.
    /// </summary>
    public static LineDiffResult GenerateDiff(string actual, string expected, StringComparison comparison = StringComparison.Ordinal, DiffOptions? options = null)
    {
        var opts = options ?? DiffOptions.Default;

        var actualLines = SplitIntoLines(actual);
        var expectedLines = SplitIntoLines(expected);

        // Check for very large inputs
        if (actualLines.Length > opts.MaxLines || expectedLines.Length > opts.MaxLines)
        {
            return GenerateSummaryDiff(actualLines, expectedLines, comparison, opts);
        }

        // Find all differences
        var changes = FindChanges(actualLines, expectedLines, comparison);

        if (changes.Count == 0)
        {
            return LineDiffResult.Equal();
        }

        // Format the diff output
        var formattedDiff = FormatDiff(actualLines, expectedLines, changes, opts);
        var firstDiffLine = changes[0].LineNumber;

        return LineDiffResult.Different(firstDiffLine, formattedDiff);
    }

    private static bool ContainsNewline(string value)
    {
        return value.Contains('\n') || value.Contains('\r');
    }

    private static string[] SplitIntoLines(string value)
    {
        // Normalize line endings and split
        return value
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n');
    }

    private static List<LineChange> FindChanges(string[] actualLines, string[] expectedLines, StringComparison comparison)
    {
        var changes = new List<LineChange>();
        var maxLength = Math.Max(actualLines.Length, expectedLines.Length);

        for (int i = 0; i < maxLength; i++)
        {
            var actualLine = i < actualLines.Length ? actualLines[i] : null;
            var expectedLine = i < expectedLines.Length ? expectedLines[i] : null;

            if (actualLine == null && expectedLine != null)
            {
                // Line was removed (only in expected)
                changes.Add(new LineChange(i + 1, LineChangeType.Removed, expectedLine, null));
            }
            else if (actualLine != null && expectedLine == null)
            {
                // Line was added (only in actual)
                changes.Add(new LineChange(i + 1, LineChangeType.Added, null, actualLine));
            }
            else if (actualLine != null && expectedLine != null && !string.Equals(actualLine, expectedLine, comparison))
            {
                // Line was modified
                changes.Add(new LineChange(i + 1, LineChangeType.Modified, expectedLine, actualLine));
            }
        }

        return changes;
    }

    private static LineDiffResult GenerateSummaryDiff(string[] actualLines, string[] expectedLines, StringComparison comparison, DiffOptions opts)
    {
        // Find first difference for summary
        var minLength = Math.Min(actualLines.Length, expectedLines.Length);

        for (int i = 0; i < minLength; i++)
        {
            if (!string.Equals(actualLines[i], expectedLines[i], comparison))
            {
                var message = $"found multiline string ({actualLines.Length} lines) with differences starting at line {i + 1} (content too large for detailed diff)";
                return LineDiffResult.Different(i + 1, message);
            }
        }

        if (actualLines.Length != expectedLines.Length)
        {
            var message = $"found multiline string with {actualLines.Length} lines but expected {expectedLines.Length} lines";
            return LineDiffResult.Different(minLength + 1, message);
        }

        return LineDiffResult.Equal();
    }

    private static string FormatDiff(string[] actualLines, string[] expectedLines, List<LineChange> changes, DiffOptions opts)
    {
        var sb = new StringBuilder();
        var maxActualLines = actualLines.Length;
        var maxExpectedLines = expectedLines.Length;
        var totalLines = Math.Max(maxActualLines, maxExpectedLines);

        sb.Append($"found multiline string with differences starting at line {changes[0].LineNumber}:");

        // Group changes into regions with context
        var regions = GroupChangesIntoRegions(changes, totalLines, opts);
        var shownDifferences = 0;
        var hitLimit = false;

        foreach (var region in regions)
        {
            if (hitLimit)
            {
                break;
            }

            sb.AppendLine();

            foreach (var lineInfo in region)
            {
                var (lineNumber, changeType, expectedLine, actualLine) = lineInfo;
                var lineNumStr = lineNumber.ToString();

                // Check if we've hit the limit before processing each change
                if (changeType != LineChangeType.Unchanged && shownDifferences >= opts.MaxDifferences)
                {
                    var remaining = changes.Count - shownDifferences;
                    sb.Append($"  ... and {remaining} more difference{(remaining == 1 ? "" : "s")}");
                    hitLimit = true;
                    break;
                }

                switch (changeType)
                {
                    case LineChangeType.Unchanged:
                        var contextLine = lineNumber <= actualLines.Length ? actualLines[lineNumber - 1] : expectedLines[lineNumber - 1];
                        sb.AppendLine($"  {lineNumStr}: {TruncateLine(contextLine, opts.MaxLineLength)}");
                        break;

                    case LineChangeType.Modified:
                        sb.AppendLine($"- {lineNumStr}: {TruncateLine(expectedLine!, opts.MaxLineLength)}");
                        sb.AppendLine($"+ {lineNumStr}: {TruncateLine(actualLine!, opts.MaxLineLength)}");
                        shownDifferences++;
                        break;

                    case LineChangeType.Added:
                        sb.AppendLine($"+ {lineNumStr}: {TruncateLine(actualLine!, opts.MaxLineLength)}");
                        shownDifferences++;
                        break;

                    case LineChangeType.Removed:
                        sb.AppendLine($"- {lineNumStr}: {TruncateLine(expectedLine!, opts.MaxLineLength)}");
                        shownDifferences++;
                        break;
                }
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static List<List<LineChange>> GroupChangesIntoRegions(List<LineChange> changes, int totalLines, DiffOptions opts)
    {
        var regions = new List<List<LineChange>>();
        if (changes.Count == 0)
        {
            return regions;
        }

        var currentRegion = new List<LineChange>();
        var changeIndex = 0;

        // Process each change, adding context lines around it
        while (changeIndex < changes.Count)
        {
            var change = changes[changeIndex];
            var contextStart = Math.Max(1, change.LineNumber - opts.ContextLines);
            var contextEnd = Math.Min(totalLines, change.LineNumber + opts.ContextLines);

            // Add context before the change
            for (int line = contextStart; line < change.LineNumber; line++)
            {
                if (!currentRegion.Any(c => c.LineNumber == line))
                {
                    currentRegion.Add(new LineChange(line, LineChangeType.Unchanged, null, null));
                }
            }

            // Add the change itself
            currentRegion.Add(change);

            // Look ahead for adjacent changes that should be in the same region
            while (changeIndex + 1 < changes.Count)
            {
                var nextChange = changes[changeIndex + 1];
                if (nextChange.LineNumber <= contextEnd + 1)
                {
                    // Fill in context lines between this change and next
                    for (int line = change.LineNumber + 1; line < nextChange.LineNumber; line++)
                    {
                        if (!currentRegion.Any(c => c.LineNumber == line))
                        {
                            currentRegion.Add(new LineChange(line, LineChangeType.Unchanged, null, null));
                        }
                    }
                    changeIndex++;
                    change = nextChange;
                    contextEnd = Math.Min(totalLines, change.LineNumber + opts.ContextLines);
                    currentRegion.Add(change);
                }
                else
                {
                    break;
                }
            }

            // Add context after the last change in this region
            for (int line = change.LineNumber + 1; line <= contextEnd; line++)
            {
                if (line <= totalLines && !currentRegion.Any(c => c.LineNumber == line))
                {
                    currentRegion.Add(new LineChange(line, LineChangeType.Unchanged, null, null));
                }
            }

            // Sort region by line number and add to regions
            currentRegion.Sort((a, b) => a.LineNumber.CompareTo(b.LineNumber));
            regions.Add(currentRegion);
            currentRegion = new List<LineChange>();
            changeIndex++;
        }

        return regions;
    }

    private static string TruncateLine(string line, int maxLength)
    {
        if (line.Length <= maxLength)
        {
            return line;
        }

        return line.Substring(0, maxLength) + "…";
    }
}
