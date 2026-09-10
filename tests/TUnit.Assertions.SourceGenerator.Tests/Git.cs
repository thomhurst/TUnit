namespace TUnit.Assertions.SourceGenerator.Tests;

internal static class Git
{
    public static DirectoryInfo SourceDirectory { get; } = new(
        Path.Combine(Sourcy.Git.RootDirectory.FullName, "src"));

    public static DirectoryInfo TestsDirectory { get; } = new(
        Path.Combine(Sourcy.Git.RootDirectory.FullName, "tests"));
}
