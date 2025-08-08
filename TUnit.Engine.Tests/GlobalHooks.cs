using CliWrap.Buffered;

namespace TUnit.Engine.Tests;

public class GlobalHooks
{
    [Before(TestSession)]
    public static async Task BuildTestProject()
    {
        var result = await CliWrap.Cli.Wrap("dotnet")
            .WithArguments(["build", "-c", GetConfiguration()])
            .WithWorkingDirectory(FileSystemHelpers.FindFile(x => x.Name == "TUnit.TestProject.csproj")!.DirectoryName!)
            .WithValidation(CliWrap.CommandResultValidation.None)
            .ExecuteBufferedAsync();

        // Only show output if the command failed
        if (result.ExitCode != 0)
        {
            Console.WriteLine(result.StandardOutput);
            Console.Error.WriteLine(result.StandardError);
            throw new InvalidOperationException($"Build failed with exit code {result.ExitCode}");
        }
    }

    private static string GetConfiguration()
    {
        var isCi = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" ||
                   Environment.GetEnvironmentVariable("CI") == "true";

        return isCi ? "Release" : "Debug";
    }
}
