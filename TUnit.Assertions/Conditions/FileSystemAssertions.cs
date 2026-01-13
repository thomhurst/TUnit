using System.ComponentModel;
using System.Text;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

// DirectoryInfo assertions - unique ones that don't conflict with DirectoryInfoAssertionExtensions
[AssertionExtension("HasFiles")]
public class DirectoryHasFilesAssertion : Assertion<DirectoryInfo>
{
    public DirectoryHasFilesAssertion(
        AssertionContext<DirectoryInfo> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DirectoryInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("directory was null"));
        }

        value.Refresh();
        if (!value.Exists)
        {
            return Task.FromResult(AssertionResult.Failed($"directory '{value.FullName}' does not exist"));
        }

        var hasFiles = value.GetFiles().Length > 0;
        if (!hasFiles)
        {
            return Task.FromResult(AssertionResult.Failed($"directory '{value.FullName}' has no files"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to have files";
}

[AssertionExtension("HasNoSubdirectories")]
public class DirectoryHasNoSubdirectoriesAssertion : Assertion<DirectoryInfo>
{
    public DirectoryHasNoSubdirectoriesAssertion(
        AssertionContext<DirectoryInfo> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DirectoryInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("directory was null"));
        }

        value.Refresh();
        if (!value.Exists)
        {
            return Task.FromResult(AssertionResult.Failed($"directory '{value.FullName}' does not exist"));
        }

        var subdirectories = value.GetDirectories();
        if (subdirectories.Length > 0)
        {
            return Task.FromResult(AssertionResult.Failed($"directory '{value.FullName}' has {subdirectories.Length} subdirectories"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to have no subdirectories";
}

// FileInfo assertions - unique ones that don't conflict with FileInfoAssertionExtensions
[AssertionExtension("IsNotSystem")]
public class FileIsNotSystemAssertion : Assertion<FileInfo>
{
    public FileIsNotSystemAssertion(
        AssertionContext<FileInfo> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<FileInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("file was null"));
        }

        value.Refresh();
        if (!value.Exists)
        {
            return Task.FromResult(AssertionResult.Failed($"file '{value.FullName}' does not exist"));
        }

        if ((value.Attributes & FileAttributes.System) == FileAttributes.System)
        {
            return Task.FromResult(AssertionResult.Failed($"file '{value.FullName}' is a system file"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be a system file";
}

[AssertionExtension("IsNotExecutable")]
public class FileIsNotExecutableAssertion : Assertion<FileInfo>
{
    public FileIsNotExecutableAssertion(
        AssertionContext<FileInfo> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<FileInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("file was null"));
        }

        value.Refresh();
        if (!value.Exists)
        {
            return Task.FromResult(AssertionResult.Failed($"file '{value.FullName}' does not exist"));
        }

        var executableExtensions = new[] { ".exe", ".bat", ".cmd", ".com", ".sh", ".ps1" };
        var isExecutable = executableExtensions.Contains(value.Extension.ToLowerInvariant());

        if (isExecutable)
        {
            return Task.FromResult(AssertionResult.Failed($"file '{value.FullName}' is executable (extension: {value.Extension})"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be executable";
}

/// <summary>
/// File and directory comparison assertions using [GenerateAssertion] for simpler code.
/// </summary>
public static partial class FileSystemComparisonAssertions
{
    /// <summary>
    /// Asserts that a file has the same binary content as another file.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to have same content as {expected}")]
    public static AssertionResult HasSameContentAs(this FileInfo value, FileInfo expected)
    {
        if (value == null)
        {
            return AssertionResult.Failed("actual file was null");
        }

        if (expected == null)
        {
            return AssertionResult.Failed("expected file was null");
        }

        value.Refresh();
        expected.Refresh();

        if (!value.Exists)
        {
            return AssertionResult.Failed($"actual file '{value.FullName}' does not exist");
        }

        if (!expected.Exists)
        {
            return AssertionResult.Failed($"expected file '{expected.FullName}' does not exist");
        }

        if (value.Length != expected.Length)
        {
            return AssertionResult.Failed($"file sizes differ: actual {value.Length} bytes, expected {expected.Length} bytes");
        }

        var actualBytes = File.ReadAllBytes(value.FullName);
        var expectedBytes = File.ReadAllBytes(expected.FullName);

        for (int i = 0; i < actualBytes.Length; i++)
        {
            if (actualBytes[i] != expectedBytes[i])
            {
                return AssertionResult.Failed($"files differ at byte position {i}: actual 0x{actualBytes[i]:X2}, expected 0x{expectedBytes[i]:X2}");
            }
        }

        return AssertionResult.Passed;
    }

    /// <summary>
    /// Asserts that a file does NOT have the same binary content as another file.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not have same content as {expected}")]
    public static AssertionResult DoesNotHaveSameContentAs(this FileInfo value, FileInfo expected)
    {
        if (value == null || expected == null)
        {
            return AssertionResult.Passed; // null is not the same as anything
        }

        value.Refresh();
        expected.Refresh();

        if (!value.Exists || !expected.Exists)
        {
            return AssertionResult.Passed; // non-existent files are not the same
        }

        if (value.Length != expected.Length)
        {
            return AssertionResult.Passed; // different sizes means different content
        }

        var actualBytes = File.ReadAllBytes(value.FullName);
        var expectedBytes = File.ReadAllBytes(expected.FullName);

        for (int i = 0; i < actualBytes.Length; i++)
        {
            if (actualBytes[i] != expectedBytes[i])
            {
                return AssertionResult.Passed; // found a difference
            }
        }

        return AssertionResult.Failed("files have identical content");
    }

    /// <summary>
    /// Asserts that a directory has the same structure (file paths) as another directory.
    /// Does not compare file contents, only the relative paths of files and subdirectories.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to have same structure as {expected}")]
    public static AssertionResult HasSameStructureAs(this DirectoryInfo value, DirectoryInfo expected)
    {
        if (value == null)
        {
            return AssertionResult.Failed("actual directory was null");
        }

        if (expected == null)
        {
            return AssertionResult.Failed("expected directory was null");
        }

        value.Refresh();
        expected.Refresh();

        if (!value.Exists)
        {
            return AssertionResult.Failed($"actual directory '{value.FullName}' does not exist");
        }

        if (!expected.Exists)
        {
            return AssertionResult.Failed($"expected directory '{expected.FullName}' does not exist");
        }

        var actualPaths = value.EnumerateFileSystemInfos("*", SearchOption.AllDirectories)
            .Select(f => GetRelativePath(value.FullName, f.FullName))
            .OrderBy(p => p)
            .ToList();
        var expectedPaths = expected.EnumerateFileSystemInfos("*", SearchOption.AllDirectories)
            .Select(f => GetRelativePath(expected.FullName, f.FullName))
            .OrderBy(p => p)
            .ToList();

        var missingInActual = expectedPaths.Except(actualPaths).ToList();
        var extraInActual = actualPaths.Except(expectedPaths).ToList();

        if (missingInActual.Count > 0 || extraInActual.Count > 0)
        {
            var message = new StringBuilder("directory structures differ:");
            if (missingInActual.Count > 0)
            {
                message.Append($" missing: [{string.Join(", ", missingInActual.Take(5))}]");
                if (missingInActual.Count > 5)
                {
                    message.Append($" (+{missingInActual.Count - 5} more)");
                }
            }
            if (extraInActual.Count > 0)
            {
                message.Append($" extra: [{string.Join(", ", extraInActual.Take(5))}]");
                if (extraInActual.Count > 5)
                {
                    message.Append($" (+{extraInActual.Count - 5} more)");
                }
            }
            return AssertionResult.Failed(message.ToString());
        }

        return AssertionResult.Passed;
    }

    /// <summary>
    /// Asserts that a directory is equivalent to another directory.
    /// Compares both the directory structure (file paths) AND file contents.
    /// This is equivalent to NUnit's DirectoryAssert.AreEqual.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be equivalent to {expected}")]
    public static AssertionResult IsEquivalentTo(this DirectoryInfo value, DirectoryInfo expected)
    {
        if (value == null)
        {
            return AssertionResult.Failed("actual directory was null");
        }

        if (expected == null)
        {
            return AssertionResult.Failed("expected directory was null");
        }

        value.Refresh();
        expected.Refresh();

        if (!value.Exists)
        {
            return AssertionResult.Failed($"actual directory '{value.FullName}' does not exist");
        }

        if (!expected.Exists)
        {
            return AssertionResult.Failed($"expected directory '{expected.FullName}' does not exist");
        }

        // First check structure
        var actualFiles = value.EnumerateFiles("*", SearchOption.AllDirectories)
            .Select(f => GetRelativePath(value.FullName, f.FullName))
            .OrderBy(p => p)
            .ToList();
        var expectedFiles = expected.EnumerateFiles("*", SearchOption.AllDirectories)
            .Select(f => GetRelativePath(expected.FullName, f.FullName))
            .OrderBy(p => p)
            .ToList();

        var missingFiles = expectedFiles.Except(actualFiles).ToList();
        var extraFiles = actualFiles.Except(expectedFiles).ToList();

        if (missingFiles.Count > 0 || extraFiles.Count > 0)
        {
            var message = new StringBuilder("directory structures differ:");
            if (missingFiles.Count > 0)
            {
                message.Append($" missing files: [{string.Join(", ", missingFiles.Take(5))}]");
                if (missingFiles.Count > 5)
                {
                    message.Append($" (+{missingFiles.Count - 5} more)");
                }
            }
            if (extraFiles.Count > 0)
            {
                message.Append($" extra files: [{string.Join(", ", extraFiles.Take(5))}]");
                if (extraFiles.Count > 5)
                {
                    message.Append($" (+{extraFiles.Count - 5} more)");
                }
            }
            return AssertionResult.Failed(message.ToString());
        }

        // Now compare file contents
        foreach (var relativePath in actualFiles)
        {
            var actualFilePath = Path.Combine(value.FullName, relativePath);
            var expectedFilePath = Path.Combine(expected.FullName, relativePath);

            var actualBytes = File.ReadAllBytes(actualFilePath);
            var expectedBytes = File.ReadAllBytes(expectedFilePath);

            if (actualBytes.Length != expectedBytes.Length)
            {
                return AssertionResult.Failed($"file '{relativePath}' sizes differ: actual {actualBytes.Length} bytes, expected {expectedBytes.Length} bytes");
            }

            for (int i = 0; i < actualBytes.Length; i++)
            {
                if (actualBytes[i] != expectedBytes[i])
                {
                    return AssertionResult.Failed($"file '{relativePath}' content differs at byte position {i}");
                }
            }
        }

        return AssertionResult.Passed;
    }

    /// <summary>
    /// Asserts that a directory is NOT equivalent to another directory.
    /// The opposite of IsEquivalentTo - passes if structure OR content differs.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not be equivalent to {expected}")]
    public static AssertionResult IsNotEquivalentTo(this DirectoryInfo value, DirectoryInfo expected)
    {
        if (value == null || expected == null)
        {
            return AssertionResult.Passed; // null is not equivalent to anything
        }

        value.Refresh();
        expected.Refresh();

        if (!value.Exists || !expected.Exists)
        {
            return AssertionResult.Passed; // non-existent directories are not equivalent
        }

        // Check structure
        var actualFiles = value.EnumerateFiles("*", SearchOption.AllDirectories)
            .Select(f => GetRelativePath(value.FullName, f.FullName))
            .OrderBy(p => p)
            .ToList();
        var expectedFiles = expected.EnumerateFiles("*", SearchOption.AllDirectories)
            .Select(f => GetRelativePath(expected.FullName, f.FullName))
            .OrderBy(p => p)
            .ToList();

        if (!actualFiles.SequenceEqual(expectedFiles))
        {
            return AssertionResult.Passed; // Different structure
        }

        // Check file contents
        foreach (var relativePath in actualFiles)
        {
            var actualFilePath = Path.Combine(value.FullName, relativePath);
            var expectedFilePath = Path.Combine(expected.FullName, relativePath);

            var actualBytes = File.ReadAllBytes(actualFilePath);
            var expectedBytes = File.ReadAllBytes(expectedFilePath);

            if (!actualBytes.SequenceEqual(expectedBytes))
            {
                return AssertionResult.Passed; // Different content
            }
        }

        return AssertionResult.Failed("directories are equivalent");
    }

    /// <summary>
    /// Gets a relative path from one path to another. This is a polyfill for Path.GetRelativePath
    /// which is not available in netstandard2.0.
    /// </summary>
    private static string GetRelativePath(string relativeTo, string path)
    {
#if NETSTANDARD2_0
        // Normalize paths
        relativeTo = Path.GetFullPath(relativeTo);
        path = Path.GetFullPath(path);

        // Ensure relativeTo ends with directory separator for proper comparison
        if (!relativeTo.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
            !relativeTo.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
        {
            relativeTo += Path.DirectorySeparatorChar;
        }

        var relativeToUri = new Uri(relativeTo);
        var pathUri = new Uri(path);

        if (relativeToUri.Scheme != pathUri.Scheme)
        {
            return path;
        }

        var relativeUri = relativeToUri.MakeRelativeUri(pathUri);
        var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        if (string.Equals(pathUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        return relativePath;
#else
        return Path.GetRelativePath(relativeTo, path);
#endif
    }
}
