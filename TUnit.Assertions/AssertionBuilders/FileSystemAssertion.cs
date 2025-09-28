using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

public enum FileSystemAssertType
{
    FileExists,
    FileDoesNotExist,
    DirectoryExists,
    DirectoryDoesNotExist,
    HasFiles,
    HasNoFiles,
    HasSubdirectories,
    HasNoSubdirectories
}

/// <summary>
/// File system assertions
/// </summary>
public class FileSystemAssertion : AssertionBase<string>
{
    private readonly FileSystemAssertType _assertType;

    public FileSystemAssertion(Func<Task<string>> pathProvider, FileSystemAssertType assertType)
        : base(pathProvider)
    {
        _assertType = assertType;
    }

    public FileSystemAssertion(Func<string> pathProvider, FileSystemAssertType assertType)
        : base(pathProvider)
    {
        _assertType = assertType;
    }

    public FileSystemAssertion(string path, FileSystemAssertType assertType)
        : base(path)
    {
        _assertType = assertType;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var path = await GetActualValueAsync();

        if (string.IsNullOrWhiteSpace(path))
        {
            return AssertionResult.Fail("Path cannot be null or empty");
        }

        switch (_assertType)
        {
            case FileSystemAssertType.FileExists:
                if (File.Exists(path))
                    return AssertionResult.Passed;
                return AssertionResult.Fail($"Expected file '{path}' to exist but it does not");

            case FileSystemAssertType.FileDoesNotExist:
                if (!File.Exists(path))
                    return AssertionResult.Passed;
                return AssertionResult.Fail($"Expected file '{path}' to not exist but it does");

            case FileSystemAssertType.DirectoryExists:
                if (Directory.Exists(path))
                    return AssertionResult.Passed;
                return AssertionResult.Fail($"Expected directory '{path}' to exist but it does not");

            case FileSystemAssertType.DirectoryDoesNotExist:
                if (!Directory.Exists(path))
                    return AssertionResult.Passed;
                return AssertionResult.Fail($"Expected directory '{path}' to not exist but it does");

            case FileSystemAssertType.HasFiles:
                if (Directory.Exists(path) && Directory.GetFiles(path).Any())
                    return AssertionResult.Passed;
                return AssertionResult.Fail($"Expected directory '{path}' to have files but it does not");

            case FileSystemAssertType.HasNoFiles:
                if (Directory.Exists(path) && !Directory.GetFiles(path).Any())
                    return AssertionResult.Passed;
                return AssertionResult.Fail($"Expected directory '{path}' to have no files but it does");

            case FileSystemAssertType.HasSubdirectories:
                if (Directory.Exists(path) && Directory.GetDirectories(path).Any())
                    return AssertionResult.Passed;
                return AssertionResult.Fail($"Expected directory '{path}' to have subdirectories but it does not");

            case FileSystemAssertType.HasNoSubdirectories:
                if (Directory.Exists(path) && !Directory.GetDirectories(path).Any())
                    return AssertionResult.Passed;
                return AssertionResult.Fail($"Expected directory '{path}' to have no subdirectories but it does");

            default:
                throw new InvalidOperationException($"Unknown assertion type: {_assertType}");
        }
    }
}

// Extension methods for file system assertions
public static class FileSystemAssertionExtensions
{
    public static FileSystemAssertion Exists(this AssertionBuilder<string> builder)
    {
        return new FileSystemAssertion(builder.ActualValueProvider, FileSystemAssertType.FileExists);
    }

    public static FileSystemAssertion DoesNotExist(this AssertionBuilder<string> builder)
    {
        return new FileSystemAssertion(builder.ActualValueProvider, FileSystemAssertType.FileDoesNotExist);
    }

    public static FileSystemAssertion DirectoryExists(this AssertionBuilder<string> builder)
    {
        return new FileSystemAssertion(builder.ActualValueProvider, FileSystemAssertType.DirectoryExists);
    }

    public static FileSystemAssertion DirectoryDoesNotExist(this AssertionBuilder<string> builder)
    {
        return new FileSystemAssertion(builder.ActualValueProvider, FileSystemAssertType.DirectoryDoesNotExist);
    }

    public static FileSystemAssertion HasFiles(this AssertionBuilder<string> builder)
    {
        return new FileSystemAssertion(builder.ActualValueProvider, FileSystemAssertType.HasFiles);
    }

    public static FileSystemAssertion HasNoFiles(this AssertionBuilder<string> builder)
    {
        return new FileSystemAssertion(builder.ActualValueProvider, FileSystemAssertType.HasNoFiles);
    }

    public static FileSystemAssertion HasSubdirectories(this AssertionBuilder<string> builder)
    {
        return new FileSystemAssertion(builder.ActualValueProvider, FileSystemAssertType.HasSubdirectories);
    }

    public static FileSystemAssertion HasNoSubdirectories(this AssertionBuilder<string> builder)
    {
        return new FileSystemAssertion(builder.ActualValueProvider, FileSystemAssertType.HasNoSubdirectories);
    }
}