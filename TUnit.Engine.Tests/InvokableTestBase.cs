using System.Runtime.CompilerServices;
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
            yield return TestMode.SingleFileApplication;
        }
    }

    private static readonly string GetEnvironmentVariable = Environment.GetEnvironmentVariable("NET_VERSION") ?? "net9.0";

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
        Console.WriteLine(@$"Mode: {testMode}");

        return testMode switch
        {
            TestMode.SourceGenerated => RunWithoutAot(filter, assertions, runOptions, assertionExpression),
            TestMode.Reflection => RunWithoutAot(filter, assertions, runOptions.WithArgument("--reflection"), assertionExpression),
            TestMode.AOT => RunWithAot(filter, assertions, runOptions, assertionExpression),
            TestMode.SingleFileApplication => RunWithSingleFile(filter, assertions, runOptions, assertionExpression),
            _ => throw new ArgumentOutOfRangeException(nameof(testMode), testMode, null)
        };
    }

    private async Task RunWithoutAot(string filter,
        List<Action<TestRun>> assertions, RunOptions runOptions, string assertionExpression)
    {
        var testProject = Sourcy.DotNet.Projects.TUnit_TestProject;
        var guid = Guid.NewGuid().ToString("N");
        var trxFilename = guid + ".trx";
        var command = Cli.Wrap("dotnet")
            .WithArguments(
                [
                    "run",
                    "--no-build",
                    "-f", GetEnvironmentVariable,
                    "--configuration", "Release",
                    "--treenode-filter", filter,
                    "--report-trx", "--report-trx-filename", trxFilename,
                    "--diagnostic-verbosity", "Debug",
                    "--diagnostic", "--diagnostic-output-fileprefix", $"log_{GetType().Name}_",
                    "--timeout", "5m",
                    // "--hangdump", "--hangdump-filename", $"hangdump.tests-{guid}.txt", "--hangdump-timeout", "3m",

                    ..runOptions.AdditionalArguments
                ]
            )
            .WithWorkingDirectory(testProject.DirectoryName!)
            .WithValidation(CommandResultValidation.None);

        var result = await command.ExecuteBufferedAsync();

        await TrxAsserter.AssertTrx(command, result, assertions, trxFilename, assertionExpression);
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
                    "--diagnostic", "--diagnostic-output-fileprefix", $"log_{GetType().Name}_AOT_",
                    "--timeout", "5m",
                    ..runOptions.AdditionalArguments
                ]
            )
            .WithValidation(CommandResultValidation.None);

        var result = await command.ExecuteBufferedAsync();

        await TrxAsserter.AssertTrx(command, result, assertions, trxFilename, assertionExpression: assertionExpression);
    }

    private async Task RunWithSingleFile(string filter,
        List<Action<TestRun>> assertions, RunOptions runOptions, string assertionExpression)
    {
        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true")
        {
            return;
        }

        var files = FindFolder(x => x.Name == "TESTPROJECT_SINGLEFILE")!
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
                    "--diagnostic", "--diagnostic-output-fileprefix", $"log_{GetType().Name}_SINGLEFILE_",
                    "--timeout", "5m",
                    ..runOptions.AdditionalArguments
                ]
            )
            .WithValidation(CommandResultValidation.None);

        var result = await command.ExecuteBufferedAsync();

        await TrxAsserter.AssertTrx(command, result, assertions, trxFilename, assertionExpression: assertionExpression);
    }

    protected static FileInfo? FindFile(Func<FileInfo, bool> predicate)
    {
        return FileSystemHelpers.FindFile(predicate);
    }

    protected static DirectoryInfo? FindFolder(Func<DirectoryInfo, bool> predicate)
    {
        return FileSystemHelpers.FindFolder(predicate);
    }
}

public record RunOptions
{
    public List<string> AdditionalArguments { get; init; } = [];

    public RunOptions WithArgument(string argument)
    {
        AdditionalArguments.Add(argument);
        return this;
    }
}
