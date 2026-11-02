using System.Runtime.CompilerServices;
using System.Text;
using CliWrap;
using CliWrap.Buffered;
using TrxTools.TrxParser;
using TUnit.Core;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

[MethodDataSource(nameof(GetTestModes))]
public abstract class InvokableTestBase(TestMode testMode)
{
    // CI runs each mode in its own pipeline module, so the Reflection run can start before the AOT
    // app is published. Unset, both modes run.
    private static readonly string? OnlyTestMode = Environment.GetEnvironmentVariable("TUNIT_ENGINE_TEST_MODE");

    public static IEnumerable<TestMode> GetTestModes()
    {
        if (OnlyTestMode is null or nameof(TestMode.Reflection))
        {
            yield return TestMode.Reflection;
        }

        if (!EnvironmentVariables.IsNetFramework && OnlyTestMode is (null or nameof(TestMode.AOT)))
        {
            yield return TestMode.AOT;
        }
    }

    private static readonly string GetEnvironmentVariable = Environment.GetEnvironmentVariable("NET_VERSION") ?? "net10.0";

    public static bool IsNetFramework => GetEnvironmentVariable.StartsWith("net4");

    protected Task RunTestsWithFilter(string filter,
        List<Action<TestRun>> assertions,
        [CallerArgumentExpression(nameof(assertions))] string assertionExpression = "")
    {
        return RunTestsWithFilter(filter, assertions, new RunOptions(), assertionExpression);
    }

    protected Task RunTestsWithFilter(string filter,
        List<Action<TestRun>> assertions, RunOptions runOptions,
        [CallerArgumentExpression(nameof(assertions))] string assertionExpression = "")
    {
        return testMode switch
        {
            TestMode.Reflection => RunWithoutAot(filter, assertions, runOptions.WithArgument("--reflection"), assertionExpression),
            TestMode.AOT => RunWithAot(filter, assertions, runOptions, assertionExpression),
            _ => throw new ArgumentOutOfRangeException(nameof(testMode), testMode, null)
        };
    }

    private async Task RunWithoutAot(string filter,
        List<Action<TestRun>> assertions, RunOptions runOptions, string assertionExpression)
    {
        var testProject = Sourcy.DotNet.Projects.TUnit_TestProject;
        var projectName = Path.GetFileNameWithoutExtension(testProject.Name);
        var binDir = new DirectoryInfo(Path.Combine(testProject.DirectoryName!, "bin", EnvironmentVariables.BuildConfiguration, GetEnvironmentVariable));

        var executable = binDir.EnumerateFiles(projectName).FirstOrDefault()
                      ?? binDir.EnumerateFiles(projectName + ".exe").First();

        var trxPath = NewTrxPath();
        var command = Cli.Wrap(executable.FullName)
            .WithArguments(
                [
                    "--treenode-filter", filter,
                    "--diagnostic-verbosity", "Debug",
                    "--diagnostic", "--diagnostic-file-prefix", $"log_{GetType().Name}_",
                    // Bounds a hung run. --hangdump would also dump it, but like --report-trx it
                    // starts the test host as a second process on every run.
                    "--timeout", "10m",

                    ..runOptions.AdditionalArguments
                ]
            )
            .WithWorkingDirectory(testProject.DirectoryName!)
            .WithEnvironmentVariables(BuildEnvironmentVariables(runOptions, trxPath))
            .WithValidation(CommandResultValidation.None);

        await RunWithFailureLogging(command, runOptions, trxPath, assertions, assertionExpression);
    }

    private async Task RunWithAot(string filter, List<Action<TestRun>> assertions,
        RunOptions runOptions, string assertionExpression)
    {
        Skip.When(Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true",
            "AOT tests are only run in GitHub Actions CI. Set GITHUB_ACTIONS=true to run locally.");

        var trxPath = NewTrxPath();

        var command = Cli.Wrap(AotApp.Value.FullName)
            .WithArguments(
                [
                    "--treenode-filter", filter,
                    "--diagnostic-verbosity", "Debug",
                    "--diagnostic", "--diagnostic-file-prefix", $"log_{GetType().Name}_AOT_",
                    // A whole-session budget, not a hang detector. The [EngineTest=Pass] run takes
                    // 3.5-5 minutes on Windows CI, so a 5m budget cancelled healthy runs mid-way
                    // through the sequential global [NotInParallel] tail.
                    "--timeout", "10m",
                    ..runOptions.AdditionalArguments
                ]
            )
            .WithEnvironmentVariables(BuildEnvironmentVariables(runOptions, trxPath))
            .WithValidation(CommandResultValidation.None);

        await RunWithFailureLogging(command, runOptions, trxPath, assertions, assertionExpression);
    }

    // Searched for once: walking the repository for it on every run cost seconds each time.
    private static readonly Lazy<FileInfo> AotApp = new(() =>
    {
        var files = FindFolder(x => x.Name == "TESTPROJECT_AOT")!
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .ToArray();

        return files.FirstOrDefault(x => x.Name == "TUnit.TestProject")
               ?? files.First(x => x.Name == "TUnit.TestProject.exe");
    });

    private static readonly string TrxDirectory = Path.Combine(
        Sourcy.DotNet.Projects.TUnit_TestProject.DirectoryName!, "TestResults", "engine-tests");

    private static string NewTrxPath() => Path.Combine(TrxDirectory, Guid.NewGuid().ToString("N") + ".trx");

    private static Dictionary<string, string?> BuildEnvironmentVariables(RunOptions runOptions, string trxPath)
    {
        var environmentVariables = EnvironmentVariables.DisableHtmlReporterForChildProcess();

        // The test project writes its TRX in-process when this is set (see InProcessTrxReporter
        // there). --report-trx would start the test host as a second process on every run.
        environmentVariables["TUNIT_ENGINE_TESTS_TRX_PATH"] = trxPath;

        foreach (var (key, value) in runOptions.EnvironmentVariables)
        {
            environmentVariables[key] = value;
        }

        return environmentVariables;
    }

    protected static FileInfo? FindFile(Func<FileInfo, bool> predicate)
    {
        return FileSystemHelpers.FindFile(predicate);
    }

    protected static DirectoryInfo? FindFolder(Func<DirectoryInfo, bool> predicate)
    {
        return FileSystemHelpers.FindFolder(predicate);
    }

    private async Task<CommandTask<BufferedCommandResult>> RunWithFailureLogging(Command command, RunOptions runOptions,
        string trxPath, List<Action<TestRun>> assertions, string assertionExpression)
    {
        var commandTask = command.ExecuteBufferedAsync
        (
            gracefulCancellationToken: runOptions.GracefulCancellationToken,
            forcefulCancellationToken: runOptions.ForcefulCancellationToken,
            standardOutputEncoding: runOptions.StandardOutputEncoding,
            standardErrorEncoding: runOptions.StandardErrorEncoding
        );

        BufferedCommandResult? commandResult = null;

        try
        {
            foreach (var onExecutingDelegate in runOptions.OnExecutingDelegates)
            {
                await onExecutingDelegate(commandTask);
            }

            commandResult = await commandTask;

            await TrxAsserter.AssertTrxAtPath(trxPath, assertions);
        }
        catch (Exception e)
        {
            throw new Exception($"""
                                 Error asserting results for {TestContext.Current!.Metadata.TestDetails.MethodMetadata.Class.Name}: {e.Message}

                                 $@"Mode: {testMode}",
                                 @$"Command Input: {command}",
                                 @$"Error: {commandResult?.StandardError}",
                                 @$"Output: {commandResult?.StandardOutput}"

                                 Expression: {assertionExpression}
                                 """);
        }

        return commandTask;
    }
}

public record RunOptions
{
    public CancellationToken GracefulCancellationToken { get; set; } = CancellationToken.None;
    public CancellationToken ForcefulCancellationToken { get; set; } = CancellationToken.None;

    public Encoding StandardOutputEncoding { get; set; } = Encoding.UTF8;
    public Encoding StandardErrorEncoding { get; set; } = Encoding.UTF8;

    public List<string> AdditionalArguments { get; init; } = [];

    public Dictionary<string, string?> EnvironmentVariables { get; init; } = [];

    public List<Func<CommandTask<BufferedCommandResult>, Task>> OnExecutingDelegates { get; init; } = [];

    public RunOptions WithArgument(string argument)
    {
        AdditionalArguments.Add(argument);
        return this;
    }

    public RunOptions WithEnvironmentVariable(string key, string? value)
    {
        EnvironmentVariables[key] = value;
        return this;
    }

    public RunOptions WithGracefulCancellationToken(CancellationToken token)
    {
        GracefulCancellationToken = token;
        return this;
    }

    public RunOptions WithForcefulCancellationToken(CancellationToken token)
    {
        ForcefulCancellationToken = token;
        return this;
    }
}
