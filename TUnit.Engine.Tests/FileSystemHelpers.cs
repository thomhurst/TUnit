namespace TUnit.Engine.Tests;

public class FileSystemHelpers
{
    public static FileInfo? FindFile(Func<FileInfo, bool> predicate)
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (!directory!.EnumerateDirectories().Any(x => x.Name == ".git"))
        {
            directory = directory.Parent;
        }
        
        return directory
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .FirstOrDefault(predicate);
    }
    
    public static DirectoryInfo? FindFolder(Func<DirectoryInfo, bool> predicate)
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (!directory!.EnumerateDirectories().Any(x => x.Name == ".git"))
        {
            directory = directory.Parent;
        }
        
        return directory
            .EnumerateDirectories("*", SearchOption.AllDirectories)
            .FirstOrDefault(predicate);
    }
}