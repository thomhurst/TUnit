using System.Globalization;
using System.Text;
using CliWrap;
using Shouldly;
using TUnit.Core.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// SIGTERM is how Unix asks a process to stop (docker stop, Kubernetes, CI cancellation). It must
/// cancel the run gracefully like Ctrl+C — After hooks get the full shutdown window — rather than
/// leave the runtime to end the process after a brief ProcessExit delay.
/// </summary>
[ExcludeOn(OS.Windows)]
public class SigtermTests
{
    private static readonly string NetVersion = Environment.GetEnvironmentVariable("NET_VERSION") ?? "net10.0";

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Sigterm_Cancels_Gracefully_And_Runs_After_Hooks(bool reflection, CancellationToken cancellationToken)
    {
        var markerDirectory = Path.Combine(Path.GetTempPath(), $"TUnit_Sigterm_{Guid.NewGuid():N}");
        var startedMarker = Path.Combine(markerDirectory, "started.txt");
        var afterMarker = Path.Combine(markerDirectory, "after.txt");

        var testProject = Sourcy.DotNet.Projects.TUnit_TestProject;
        var executable = Path.Combine(testProject.DirectoryName!, "bin", "Release", NetVersion, "TUnit.TestProject");

        var environmentVariables = EnvironmentVariables.DisableHtmlReporterForChildProcess();
        environmentVariables["TUNIT_SIGTERM_MARKER_DIR"] = markerDirectory;

        var output = new StringBuilder();
        using var forcefulCancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        var commandTask = Cli.Wrap(executable)
            .WithArguments(["--treenode-filter", "/*/*/SigtermCancellationTests/*", .. reflection ? ["--reflection"] : Array.Empty<string>()])
            .WithWorkingDirectory(testProject.DirectoryName!)
            .WithEnvironmentVariables(environmentVariables)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(output))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync(forcefulCancellation.Token);

        try
        {
            while (!File.Exists(startedMarker))
            {
                commandTask.Task.IsCompleted.ShouldBeFalse($"The test process exited before the test started:{Environment.NewLine}{output}");
                await Task.Delay(100, cancellationToken);
            }

            await Cli.Wrap("kill")
                .WithArguments(["-TERM", commandTask.ProcessId.ToString(CultureInfo.InvariantCulture)])
                .ExecuteAsync(cancellationToken);

            var result = await commandTask;

            File.Exists(afterMarker).ShouldBeTrue($"The After hook did not complete:{Environment.NewLine}{output}");
            output.ToString().ShouldContain("The test run was cancelled by SIGTERM.");

            // A failed session; not 143 (killed by SIGTERM itself) or 134 (aborted on an unhandled exception).
            result.ExitCode.ShouldBe(10);
        }
        finally
        {
            // If the test bailed out before the child finished, stop it before deleting the
            // directory its After hook writes to.
            if (!commandTask.Task.IsCompleted)
            {
                forcefulCancellation.Cancel();
                try
                {
                    await commandTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected: the child was killed.
                }
            }

            if (Directory.Exists(markerDirectory))
            {
                Directory.Delete(markerDirectory, recursive: true);
            }
        }
    }
}
