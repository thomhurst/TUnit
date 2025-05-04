// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace TUnit.Assertions.SourceGenerator.Helpers;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class StringCaseExtensions {
    private static Regex NonAlphanumericRegex { get; } = new("(?<=[a-z])(?=[A-Z0-9])|(?<=[0-9])(?=[a-zA-Z])|[^a-zA-Z0-9]+", RegexOptions.Compiled);
    
    
    public static string ToSpaceSeperated(this string input) {
        if (string.IsNullOrWhiteSpace(input)) return input;
        
        ReadOnlySpan<string> words = NonAlphanumericRegex.Split(input);

        Span<char> result = stackalloc char[input.Length * 2]; // Overallocate to accommodate separators
        var position = 0;

        foreach (string t1 in words) {
            if (string.IsNullOrEmpty(t1)) continue;

            var wordSpan = t1.AsSpan();

            // add seperator
            if (position > 0) result[position++] = ' ';

            // Append the word in lowercase
            foreach (char t in wordSpan) {
                result[position++] = char.ToLower(t);
            }
        }

        return new string(result[..position].ToArray());
    }
}