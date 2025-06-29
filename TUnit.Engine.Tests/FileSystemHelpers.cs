namespace TUnit.Engine.Tests;

public class FileSystemHelpers
{
    public static FileInfo? FindFile(Func<FileInfo, bool> predicate)
    {
        return Sourcy.Git.RootDirectory
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .FirstOrDefault(predicate);
    }

    public static DirectoryInfo? FindFolder(Func<DirectoryInfo, bool> predicate)
    {
        return Sourcy.Git.RootDirectory
            .EnumerateDirectories("*", SearchOption.AllDirectories)
            .FirstOrDefault(predicate);
    }
}
