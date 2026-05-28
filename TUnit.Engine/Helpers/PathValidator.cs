#if NET8_0_OR_GREATER
using System.Buffers;
#endif

namespace TUnit.Engine.Helpers;

internal static class PathValidator
{
#if NET8_0_OR_GREATER
    private static readonly SearchValues<char> _invalidFileNameChars =
        SearchValues.Create(Path.GetInvalidFileNameChars());
#endif

    /// <summary>
    /// Strips characters that are invalid in a file name component (those returned by
    /// <see cref="Path.GetInvalidFileNameChars"/>). This is not full path-traversal protection
    /// — e.g. on Linux only <c>/</c> and <c>\0</c> are invalid, so a value like <c>..foo</c>
    /// passes through unchanged; use <see cref="ValidateAndNormalizePath"/> for that.
    /// Equivalent to <c>string.Concat(name.Split(Path.GetInvalidFileNameChars()))</c> but
    /// allocation-free when the name is already clean.
    /// </summary>
    internal static string SanitizeFileName(string name)
    {
#if NET8_0_OR_GREATER
        var span = name.AsSpan();
        var firstInvalid = span.IndexOfAny(_invalidFileNameChars);

        // Fast path: nothing to strip, return the original string unchanged.
        if (firstInvalid < 0)
        {
            return name;
        }

        // Worst case the result is the same length as the input (no chars removed
        // after the first), so a single stack/heap buffer of that size suffices.
        var buffer = name.Length <= 256 ? stackalloc char[name.Length] : new char[name.Length];

        // Everything before the first invalid char is known-good.
        span.Slice(0, firstInvalid).CopyTo(buffer);
        var written = firstInvalid;

        for (var i = firstInvalid; i < span.Length; i++)
        {
            var c = span[i];
            if (!_invalidFileNameChars.Contains(c))
            {
                buffer[written++] = c;
            }
        }

        return new string(buffer.Slice(0, written));
#else
        return string.Concat(name.Split(Path.GetInvalidFileNameChars()));
#endif
    }

    /// <summary>
    /// Validates and normalizes a file path to prevent path traversal attacks.
    /// Returns the normalized full path if valid, or throws an <see cref="ArgumentException"/> if the path is unsafe.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <param name="parameterName">The name of the parameter for error messages.</param>
    /// <returns>The normalized, validated full path.</returns>
    /// <exception cref="ArgumentException">Thrown when the path is null, empty, or contains path traversal sequences.</exception>
    internal static string ValidateAndNormalizePath(string? path, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty.", parameterName);
        }

        // At this point path is guaranteed non-null and non-whitespace
        var validatedPath = path!;

        // Reject paths containing path traversal sequences before normalization
        // This catches attempts like "../../etc/passwd" or "foo/..\\bar"
        if (ContainsPathTraversal(validatedPath))
        {
            throw new ArgumentException(
                $"Path contains path traversal sequences and is not allowed: '{validatedPath}'",
                parameterName);
        }

        // Normalize the path to resolve any remaining relative segments
        var fullPath = Path.GetFullPath(validatedPath);

        // After normalization, verify the result doesn't differ from what we'd expect
        // (e.g., a crafted path that sneaks through the string check)
        // The normalized path should not escape above the current working directory
        // for relative paths, or should be a valid absolute path
        if (!Path.IsPathRooted(validatedPath))
        {
            var currentDir = Path.GetFullPath(Directory.GetCurrentDirectory());

            if (!fullPath.StartsWith(currentDir, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Relative path resolves outside the current working directory and is not allowed: '{validatedPath}'",
                    parameterName);
            }
        }

        return fullPath;
    }

    private static bool ContainsPathTraversal(string path)
    {
        // Check for ".." segments which indicate path traversal
        // We need to check both forward and backward slash separators
        var normalized = path.Replace('\\', '/');

        // Split on '/' and check for ".." segments
        var segments = normalized.Split('/');

        foreach (var segment in segments)
        {
            if (segment == "..")
            {
                return true;
            }
        }

        return false;
    }
}
