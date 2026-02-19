using System.Runtime.CompilerServices;
using System.Text;
using CliWrap;
using CliWrap.Buffered;
using TrxTools.TrxParser;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

[MethodDataSource(nameof(GetTestModes))]
public abstract class InvokableTestBase(TestMode testMode)
{
    public static IEnumerable<TestMode> GetTestModes()
    {
        yield return TestMode.SourceGenerated;
        yield return TestMode.Reflection;

        if (!EnvironmentVariables.IsNetFramework)
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
            TestMode.SourceGenerated => RunWithoutAot(filter, assertions, runOptions, assertionExpression),
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
        var binDir = new DirectoryInfo(Path.Combine(testProject.DirectoryName!, "bin", "Release", GetEnvironmentVariable));

        var executable = binDir.EnumerateFiles(projectName).FirstOrDefault()
                      ?? binDir.EnumerateFiles(projectName + ".exe").First();

        var guid = Guid.NewGuid().ToString("N");
        var trxFilename = guid + ".trx";
        var command = Cli.Wrap(executable.FullName)
            .WithArguments(
                [
                    "--treenode-filter", filter,
                    "--report-trx", "--report-trx-filename", trxFilename,
                    "--diagnostic-verbosity", "Debug",
                    "--diagnostic", "--diagnostic-file-prefix", $"log_{GetType().Name}_",
                    "--hangdump", "--hangdump-filename", $"hangdump.{Environment.OSVersion.Platform}.tests-{guid}.dmp", "--hangdump-timeout", "5m",

                    ..runOptions.AdditionalArguments
                ]
            )
            .WithWorkingDirectory(testProject.DirectoryName!)
            .WithValidation(CommandResultValidation.None);

        await RunWithFailureLogging(command, runOptions, trxFilename, assertions, assertionExpression);
    }

    private async Task RunWithAot(string filter, List<Action<TestRun>> assertions,
        RunOptions runOptions, string assertionExpression)
    {
        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true")
        {
            return;
        }

        var files = FindFolder(x => x.Name == "TESTPROJECT_AOT")!
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .ToArray();

        var aotApp = files.FirstOrDefault(x => x.Name == "TUnit.TestProject")
                     ?? files.First(x => x.Name == "TUnit.TestProject.exe");

        var guid = Guid.NewGuid().ToString("N");
        var trxFilename = guid + ".trx";

        var command = Cli.Wrap(aotApp.FullName)
            .WithArguments(
                [
                    "--treenode-filter", filter,
                    "--report-trx", "--report-trx-filename", trxFilename,
                    "--diagnostic-verbosity", "Debug",
                    "--diagnostic", "--diagnostic-file-prefix", $"log_{GetType().Name}_AOT_",
                    "--timeout", "5m",
                    ..runOptions.AdditionalArguments
                ]
            )
            .WithValidation(CommandResultValidation.None);

        await RunWithFailureLogging(command, runOptions, trxFilename, assertions, assertionExpression);
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
        string trxFilename, List<Action<TestRun>> assertions, string assertionExpression)
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

            await TrxAsserter.AssertTrx(testMode, command, commandResult, assertions, trxFilename, assertionExpression: assertionExpression);
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

    public List<Func<CommandTask<BufferedCommandResult>, Task>> OnExecutingDelegates { get; init; } = [];

    public RunOptions WithArgument(string argument)
    {
        AdditionalArguments.Add(argument);
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
