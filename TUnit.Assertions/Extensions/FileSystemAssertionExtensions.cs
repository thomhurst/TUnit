using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

public static class FileSystemAssertionExtensions
{
    // DirectoryInfo extensions
    public static DirectoryExistsAssertion Exists(
        this IAssertionSource<DirectoryInfo> source)
    {
        source.ExpressionBuilder.Append(".Exists()");
        return new DirectoryExistsAssertion(source.Context, source.ExpressionBuilder);
    }

    public static DirectoryDoesNotExistAssertion DoesNotExist(
        this IAssertionSource<DirectoryInfo> source)
    {
        source.ExpressionBuilder.Append(".DoesNotExist()");
        return new DirectoryDoesNotExistAssertion(source.Context, source.ExpressionBuilder);
    }

    public static DirectoryIsNotEmptyAssertion IsNotEmpty(
        this IAssertionSource<DirectoryInfo> source)
    {
        source.ExpressionBuilder.Append(".IsNotEmpty()");
        return new DirectoryIsNotEmptyAssertion(source.Context, source.ExpressionBuilder);
    }

    public static DirectoryHasFilesAssertion HasFiles(
        this IAssertionSource<DirectoryInfo> source)
    {
        source.ExpressionBuilder.Append(".HasFiles()");
        return new DirectoryHasFilesAssertion(source.Context, source.ExpressionBuilder);
    }

    public static DirectoryHasNoSubdirectoriesAssertion HasNoSubdirectories(
        this IAssertionSource<DirectoryInfo> source)
    {
        source.ExpressionBuilder.Append(".HasNoSubdirectories()");
        return new DirectoryHasNoSubdirectoriesAssertion(source.Context, source.ExpressionBuilder);
    }

    // FileInfo extensions
    public static FileExistsAssertion Exists(
        this IAssertionSource<FileInfo> source)
    {
        source.ExpressionBuilder.Append(".Exists()");
        return new FileExistsAssertion(source.Context, source.ExpressionBuilder);
    }

    public static FileDoesNotExistAssertion DoesNotExist(
        this IAssertionSource<FileInfo> source)
    {
        source.ExpressionBuilder.Append(".DoesNotExist()");
        return new FileDoesNotExistAssertion(source.Context, source.ExpressionBuilder);
    }

    public static FileIsNotEmptyAssertion IsNotEmpty(
        this IAssertionSource<FileInfo> source)
    {
        source.ExpressionBuilder.Append(".IsNotEmpty()");
        return new FileIsNotEmptyAssertion(source.Context, source.ExpressionBuilder);
    }

    public static FileIsNotReadOnlyAssertion IsNotReadOnly(
        this IAssertionSource<FileInfo> source)
    {
        source.ExpressionBuilder.Append(".IsNotReadOnly()");
        return new FileIsNotReadOnlyAssertion(source.Context, source.ExpressionBuilder);
    }

    public static FileIsNotHiddenAssertion IsNotHidden(
        this IAssertionSource<FileInfo> source)
    {
        source.ExpressionBuilder.Append(".IsNotHidden()");
        return new FileIsNotHiddenAssertion(source.Context, source.ExpressionBuilder);
    }

    public static FileIsNotSystemAssertion IsNotSystem(
        this IAssertionSource<FileInfo> source)
    {
        source.ExpressionBuilder.Append(".IsNotSystem()");
        return new FileIsNotSystemAssertion(source.Context, source.ExpressionBuilder);
    }

    public static FileIsNotExecutableAssertion IsNotExecutable(
        this IAssertionSource<FileInfo> source)
    {
        source.ExpressionBuilder.Append(".IsNotExecutable()");
        return new FileIsNotExecutableAssertion(source.Context, source.ExpressionBuilder);
    }
}
