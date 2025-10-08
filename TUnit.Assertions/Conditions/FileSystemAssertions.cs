using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

// DirectoryInfo assertions
public class DirectoryExistsAssertion : Assertion<DirectoryInfo>
{
    public DirectoryExistsAssertion(
        EvaluationContext<DirectoryInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DirectoryInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("directory was null"));

        value.Refresh();
        if (!value.Exists)
            return Task.FromResult(AssertionResult.Failed($"directory '{value.FullName}' does not exist"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to exist";
}

public class DirectoryDoesNotExistAssertion : Assertion<DirectoryInfo>
{
    public DirectoryDoesNotExistAssertion(
        EvaluationContext<DirectoryInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DirectoryInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Passed);

        value.Refresh();
        if (value.Exists)
            return Task.FromResult(AssertionResult.Failed($"directory '{value.FullName}' exists"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not exist";
}

public class DirectoryIsNotEmptyAssertion : Assertion<DirectoryInfo>
{
    public DirectoryIsNotEmptyAssertion(
        EvaluationContext<DirectoryInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DirectoryInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("directory was null"));

        value.Refresh();
        if (!value.Exists)
            return Task.FromResult(AssertionResult.Failed($"directory '{value.FullName}' does not exist"));

        var hasContent = value.GetFileSystemInfos().Length > 0;
        if (!hasContent)
            return Task.FromResult(AssertionResult.Failed($"directory '{value.FullName}' is empty"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be empty";
}

public class DirectoryHasFilesAssertion : Assertion<DirectoryInfo>
{
    public DirectoryHasFilesAssertion(
        EvaluationContext<DirectoryInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DirectoryInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("directory was null"));

        value.Refresh();
        if (!value.Exists)
            return Task.FromResult(AssertionResult.Failed($"directory '{value.FullName}' does not exist"));

        var hasFiles = value.GetFiles().Length > 0;
        if (!hasFiles)
            return Task.FromResult(AssertionResult.Failed($"directory '{value.FullName}' has no files"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to have files";
}

public class DirectoryHasNoSubdirectoriesAssertion : Assertion<DirectoryInfo>
{
    public DirectoryHasNoSubdirectoriesAssertion(
        EvaluationContext<DirectoryInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DirectoryInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("directory was null"));

        value.Refresh();
        if (!value.Exists)
            return Task.FromResult(AssertionResult.Failed($"directory '{value.FullName}' does not exist"));

        var subdirectories = value.GetDirectories();
        if (subdirectories.Length > 0)
            return Task.FromResult(AssertionResult.Failed($"directory '{value.FullName}' has {subdirectories.Length} subdirectories"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to have no subdirectories";
}

// FileInfo assertions
public class FileExistsAssertion : Assertion<FileInfo>
{
    public FileExistsAssertion(
        EvaluationContext<FileInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<FileInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("file was null"));

        value.Refresh();
        if (!value.Exists)
            return Task.FromResult(AssertionResult.Failed($"file '{value.FullName}' does not exist"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to exist";
}

public class FileDoesNotExistAssertion : Assertion<FileInfo>
{
    public FileDoesNotExistAssertion(
        EvaluationContext<FileInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<FileInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Passed);

        value.Refresh();
        if (value.Exists)
            return Task.FromResult(AssertionResult.Failed($"file '{value.FullName}' exists"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not exist";
}

public class FileIsNotEmptyAssertion : Assertion<FileInfo>
{
    public FileIsNotEmptyAssertion(
        EvaluationContext<FileInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<FileInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("file was null"));

        value.Refresh();
        if (!value.Exists)
            return Task.FromResult(AssertionResult.Failed($"file '{value.FullName}' does not exist"));

        if (value.Length == 0)
            return Task.FromResult(AssertionResult.Failed($"file '{value.FullName}' is empty"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be empty";
}

public class FileIsNotReadOnlyAssertion : Assertion<FileInfo>
{
    public FileIsNotReadOnlyAssertion(
        EvaluationContext<FileInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<FileInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("file was null"));

        value.Refresh();
        if (!value.Exists)
            return Task.FromResult(AssertionResult.Failed($"file '{value.FullName}' does not exist"));

        if (value.IsReadOnly)
            return Task.FromResult(AssertionResult.Failed($"file '{value.FullName}' is read-only"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be read-only";
}

public class FileIsNotHiddenAssertion : Assertion<FileInfo>
{
    public FileIsNotHiddenAssertion(
        EvaluationContext<FileInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<FileInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("file was null"));

        value.Refresh();
        if (!value.Exists)
            return Task.FromResult(AssertionResult.Failed($"file '{value.FullName}' does not exist"));

        if ((value.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
            return Task.FromResult(AssertionResult.Failed($"file '{value.FullName}' is hidden"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be hidden";
}

public class FileIsNotSystemAssertion : Assertion<FileInfo>
{
    public FileIsNotSystemAssertion(
        EvaluationContext<FileInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<FileInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("file was null"));

        value.Refresh();
        if (!value.Exists)
            return Task.FromResult(AssertionResult.Failed($"file '{value.FullName}' does not exist"));

        if ((value.Attributes & FileAttributes.System) == FileAttributes.System)
            return Task.FromResult(AssertionResult.Failed($"file '{value.FullName}' is a system file"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be a system file";
}

public class FileIsNotExecutableAssertion : Assertion<FileInfo>
{
    public FileIsNotExecutableAssertion(
        EvaluationContext<FileInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<FileInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("file was null"));

        value.Refresh();
        if (!value.Exists)
            return Task.FromResult(AssertionResult.Failed($"file '{value.FullName}' does not exist"));

        var executableExtensions = new[] { ".exe", ".bat", ".cmd", ".com", ".sh", ".ps1" };
        var isExecutable = executableExtensions.Contains(value.Extension.ToLowerInvariant());

        if (isExecutable)
            return Task.FromResult(AssertionResult.Failed($"file '{value.FullName}' is executable (extension: {value.Extension})"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be executable";
}
