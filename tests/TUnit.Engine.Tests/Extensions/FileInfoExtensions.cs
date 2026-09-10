namespace TUnit.Engine.Tests.Extensions;

public static class FileInfoExtensions
{
    public static FileInfo AssertExists(this FileInfo? fileInfo)
    {
        if (fileInfo is null || !fileInfo.Exists)
        {
            throw new FileNotFoundException($"The file {fileInfo?.FullName} does not exist.");
        }

        return fileInfo;
    }
}
