namespace TUnit.Core.SourceGenerator.Tests.Extensions;

public static class DirectoryInfoExtensions
{
    public static DirectoryInfo GetDirectory(this DirectoryInfo directory, string name)
    {
        var subDirectory = directory.GetDirectories(name).FirstOrDefault();

        if (subDirectory is null)
        {
            throw new DirectoryNotFoundException($"Directory '{name}' not found in '{directory.FullName}'.");
        }

        return subDirectory;
    }
}
