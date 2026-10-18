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

    /// <summary>The test host is built and run in the same configuration as this suite.</summary>
#if DEBUG
    public const string Configuration = "Debug";
#else
    public const string Configuration = "Release";
#endif

    // Set by the CI pipeline, whose solution build has already produced the test host.
    private static readonly bool IsPrebuilt =
        Environment.GetEnvironmentVariable("TUNIT_RPC_TESTPROJECT_PREBUILT") == "true";

    public static Task EnsureBuiltAsync(string framework, CancellationToken cancellationToken)
        => IsPrebuilt ? Task.CompletedTask : _builds.GetOrAdd(framework, tfm => BuildAsync(tfm, cancellationToken));

    private static async Task BuildAsync(string framework, CancellationToken cancellationToken)
    {
        var result = await Cli.Wrap(DotNetCli.Path)
            .WithWorkingDirectory(WorkingDirectory)
            .WithArguments(["build", "-c", Configuration, "-f", framework, "--nologo", "-v", "quiet"])
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Test host pre-build ({framework}) failed with exit code {result.ExitCode} using '{DotNetCli.Path}':{Environment.NewLine}{result.StandardOutput}{result.StandardError}");
        }
    }
}
