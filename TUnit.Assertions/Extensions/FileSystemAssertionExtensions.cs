using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

public static class FileSystemAssertionExtensions
{
    // DirectoryInfo extensions
    public static DirectoryExistsAssertion Exists(
        this IAssertionSource<DirectoryInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".Exists()");
        return new DirectoryExistsAssertion(source.Context);
    }

    public static DirectoryDoesNotExistAssertion DoesNotExist(
        this IAssertionSource<DirectoryInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".DoesNotExist()");
        return new DirectoryDoesNotExistAssertion(source.Context);
    }

    public static DirectoryIsNotEmptyAssertion IsNotEmpty(
        this IAssertionSource<DirectoryInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotEmpty()");
        return new DirectoryIsNotEmptyAssertion(source.Context);
    }

    public static DirectoryHasFilesAssertion HasFiles(
        this IAssertionSource<DirectoryInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".HasFiles()");
        return new DirectoryHasFilesAssertion(source.Context);
    }

    public static DirectoryHasNoSubdirectoriesAssertion HasNoSubdirectories(
        this IAssertionSource<DirectoryInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".HasNoSubdirectories()");
        return new DirectoryHasNoSubdirectoriesAssertion(source.Context);
    }

    // FileInfo extensions
    public static FileExistsAssertion Exists(
        this IAssertionSource<FileInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".Exists()");
        return new FileExistsAssertion(source.Context);
    }

    public static FileDoesNotExistAssertion DoesNotExist(
        this IAssertionSource<FileInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".DoesNotExist()");
        return new FileDoesNotExistAssertion(source.Context);
    }

    public static FileIsNotEmptyAssertion IsNotEmpty(
        this IAssertionSource<FileInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotEmpty()");
        return new FileIsNotEmptyAssertion(source.Context);
    }

    public static FileIsNotReadOnlyAssertion IsNotReadOnly(
        this IAssertionSource<FileInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotReadOnly()");
        return new FileIsNotReadOnlyAssertion(source.Context);
    }

    public static FileIsNotHiddenAssertion IsNotHidden(
        this IAssertionSource<FileInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotHidden()");
        return new FileIsNotHiddenAssertion(source.Context);
    }

    public static FileIsNotSystemAssertion IsNotSystem(
        this IAssertionSource<FileInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotSystem()");
        return new FileIsNotSystemAssertion(source.Context);
    }

    public static FileIsNotExecutableAssertion IsNotExecutable(
        this IAssertionSource<FileInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotExecutable()");
        return new FileIsNotExecutableAssertion(source.Context);
    }
}
