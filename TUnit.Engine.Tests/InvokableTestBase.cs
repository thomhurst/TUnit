using System.Runtime.CompilerServices;
using CliWrap;
using CliWrap.Buffered;
using TrxTools.TrxParser;

namespace TUnit.Engine.Tests;

public abstract class InvokableTestBase
{
    private static readonly string GetEnvironmentVariable = Environment.GetEnvironmentVariable("NET_VERSION") ?? "net9.0";

    public static bool IsNetFramework => GetEnvironmentVariable == "net472";
    
    protected Task RunTestsWithFilter(string filter,
        List<Action<TestRun>> assertions,
        [CallerArgumentExpression(nameof(assertions))] string assertionExpression = "")
    {
        return RunTestsWithFilter(filter, assertions, new RunOptions(), assertionExpression);
    }

    protected async Task RunTestsWithFilter(string filter,
        List<Action<TestRun>> assertions, RunOptions runOptions,
        [CallerArgumentExpression(nameof(assertions))] string assertionExpression = "")
    {
        await RunWithoutAot(filter, assertions, runOptions, assertionExpression);

        if (EnvironmentVariables.IsNet472)
        {
            return;
        }
        
        await RunWithAot(filter, assertions, runOptions, assertionExpression);

        await RunWithSingleFile(filter, assertions, runOptions, assertionExpression);
    }

    private async Task RunWithoutAot(string filter,
        List<Action<TestRun>> assertions, RunOptions runOptions, string assertionExpression)
    {
        var testProject = Sourcy.DotNet.Projects.TUnit_TestProject;
        var guid = Guid.NewGuid().ToString("N");
        var trxFilename = guid + ".trx";
        var result = await Cli.Wrap("dotnet")
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
                    "--hangdump", "--hangdump-filename", $"hangdump.tests-{guid}.txt", "--hangdump-timeout", "3m",

                    ..runOptions.AdditionalArguments
                ]
            )
            .WithWorkingDirectory(testProject.DirectoryName!)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        await AssertTrx(result, assertions, trxFilename, assertionExpression);
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
        
        var result = await Cli.Wrap(aotApp.FullName)
            .WithArguments(
                [
                    "--treenode-filter", filter,
                    "--report-trx", "--report-trx-filename", trxFilename,
                    "--diagnostic", "--diagnostic-output-fileprefix", $"log_{GetType().Name}_AOT_", 
                    "--timeout", "5m",
                    ..runOptions.AdditionalArguments
                ]
            )
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        await AssertTrx(result, assertions, trxFilename, assertionExpression);
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
        
        var result = await Cli.Wrap(aotApp.FullName)
            .WithArguments(
                [
                    "--treenode-filter", filter,
                    "--report-trx", "--report-trx-filename", trxFilename,
                    "--diagnostic", "--diagnostic-output-fileprefix", $"log_{GetType().Name}_SINGLEFILE_", 
                    "--timeout", "5m",
                    ..runOptions.AdditionalArguments
                ]
            )
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        await AssertTrx(result, assertions, trxFilename, assertionExpression);
    }

    protected static FileInfo? FindFile(Func<FileInfo, bool> predicate)
    {
        return FileSystemHelpers.FindFile(predicate);
    }
    
    protected static DirectoryInfo? FindFolder(Func<DirectoryInfo, bool> predicate)
    {
        return FileSystemHelpers.FindFolder(predicate);
    }

    private async Task AssertTrx(BufferedCommandResult commandResult,
        List<Action<TestRun>> assertions,
        string trxFilename, string assertionExpression)
    {
        try
        {
            var trxFile = FindFile(x => x.Name == trxFilename)?.FullName ?? throw new FileNotFoundException($"Could not find trx file {trxFilename}");
            
            var trxFileContents = await File.ReadAllTextAsync(trxFile);

            var testRun = TrxControl.ReadTrx(new StringReader(trxFileContents));
            
            assertions.ForEach(x => x.Invoke(testRun));
        }
        catch (Exception e)
        {
            Console.WriteLine(@$"Command Input: {commandResult}");
            Console.WriteLine(@$"Error: {commandResult.StandardError}");
            Console.WriteLine(@$"Output: {commandResult.StandardOutput}");

            throw new Exception($"""
                                 Error asserting results for {GetType().Name}: {e.Message}

                                 Expression: {assertionExpression}
                                 """, e);
        }
    }
}

public record RunOptions
{
    public List<string> AdditionalArguments { get; init; } = [];
}
