namespace TUnit.Engine.Tests;

public class GlobalHooks
{
    [Before(TestSession)]
    public static async Task BuildTestProject()
    {
        await CliWrap.Cli.Wrap("dotnet")
            .WithArguments(["build", "-c", "Release"])
            .WithWorkingDirectory(FileSystemHelpers.FindFile(x => x.Name == "TUnit.TestProject.csproj")!.DirectoryName!)
            .ExecuteAsync();
    }
}
