using CliWrap;
using CliWrap.Buffered;
using Shouldly;

namespace TUnit.Engine.Tests;

/// <summary>
/// Verifies that the built-in <c>--list-tests</c> flag honours <c>--treenode-filter</c>.
/// Listing routes through <see cref="TUnit.Engine.TestDiscoveryService"/> as a discovery request,
/// so this exercises the same engine code path in both reflection and source-generated modes.
///
/// Note: this drives the built test-project executable directly rather than using
/// <see cref="InvokableTestBase"/>, because that harness always attaches <c>--report-trx</c> and
/// asserts a TRX, which <c>--list-tests</c> does not produce. We assert on stdout instead.
/// </summary>
public class ListTestsFilterTests
{
    private static readonly string NetVersion = Environment.GetEnvironmentVariable("NET_VERSION") ?? "net10.0";

    // true => reflection mode (--reflection), false => default source-generated mode.
    public static IEnumerable<bool> Modes()
    {
        yield return false;
        yield return true;
    }

    [Test]
    [MethodDataSource(nameof(Modes))]
    public async Task ListTests_With_Filter_Lists_Only_Matching_Tests(bool reflection)
    {
        var result = await RunListTestsAsync(reflection, "/*/*/PassFailTests/*[Category=Pass]");

        // Pass* tests match the filter and should be listed...
        result.StandardOutput.ShouldContain("Pass1");
        // ...while Fail* tests (Category=Fail) are excluded and must NOT appear.
        result.StandardOutput.ShouldNotContain("Fail1");
    }

    [Test]
    [MethodDataSource(nameof(Modes))]
    public async Task ListTests_Without_Filter_Lists_Everything(bool reflection)
    {
        // Guards the no-filter branch: bare --list-tests (null/NopFilter) must keep listing
        // every test, so both Pass* and Fail* appear.
        var result = await RunListTestsAsync(reflection, filter: null);

        result.StandardOutput.ShouldContain("Pass1");
        result.StandardOutput.ShouldContain("Fail1");
    }

    private static async Task<BufferedCommandResult> RunListTestsAsync(bool reflection, string? filter)
    {
        var testProject = Sourcy.DotNet.Projects.tests__TUnit_TestProject;
        var projectName = Path.GetFileNameWithoutExtension(testProject.Name);
        var binDir = new DirectoryInfo(Path.Combine(testProject.DirectoryName!, "bin", "Release", NetVersion));

        var executable = binDir.EnumerateFiles(projectName).FirstOrDefault()
                      ?? binDir.EnumerateFiles(projectName + ".exe").First();

        var arguments = new List<string> { "--list-tests" };

        if (filter is not null)
        {
            arguments.Add("--treenode-filter");
            arguments.Add(filter);
        }

        if (reflection)
        {
            arguments.Add("--reflection");
        }

        return await Cli.Wrap(executable.FullName)
            .WithArguments(arguments)
            .WithWorkingDirectory(testProject.DirectoryName!)
            .WithEnvironmentVariables(new Dictionary<string, string?> { ["TUNIT_DISABLE_HTML_REPORTER"] = "true" })
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();
    }
}
