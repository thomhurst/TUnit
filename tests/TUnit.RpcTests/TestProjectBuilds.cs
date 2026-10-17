using System.Collections.Concurrent;
using CliWrap;
using CliWrap.Buffered;
using TUnit.Tests.Shared;

namespace TUnit.RpcTests;

/// <summary>
/// Memoizes pre-builds of TUnit.TestProject keyed by TFM so parallel / repeat
/// test runs for the same framework share a single <c>dotnet build</c>.
/// </summary>
internal static class TestProjectBuilds
{
    private static readonly ConcurrentDictionary<string, Task> _builds = new();

    public static string WorkingDirectory { get; } = Sourcy.DotNet.Projects.TUnit_TestProject.DirectoryName!;

    public static Task EnsureBuiltAsync(string framework, CancellationToken cancellationToken)
        => _builds.GetOrAdd(framework, tfm => BuildAsync(tfm, cancellationToken));

    private static async Task BuildAsync(string framework, CancellationToken cancellationToken)
    {
        var result = await Cli.Wrap(DotNetCli.Path)
            .WithWorkingDirectory(WorkingDirectory)
            .WithArguments(["build", "-c", "Debug", "-f", framework, "--nologo", "-v", "quiet"])
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Test host pre-build ({framework}) failed with exit code {result.ExitCode} using '{DotNetCli.Path}':{Environment.NewLine}{result.StandardOutput}{result.StandardError}");
        }
    }
}
