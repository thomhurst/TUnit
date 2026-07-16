namespace TUnit.Core.SourceGenerator.Tests;

internal static class Git
{
    public static DirectoryInfo TestsDirectory { get; } = new(
        Path.Combine(Sourcy.Git.RootDirectory.FullName, "tests"));
}
