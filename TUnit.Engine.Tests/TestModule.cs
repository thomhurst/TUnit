using System.Runtime.CompilerServices;
using CliWrap;
using CliWrap.Buffered;
using FluentAssertions.Execution;
using TrxTools.TrxParser;

namespace TUnit.Engine.Tests;

public abstract class TestModule
{
    protected Task RunTestsWithFilter(string filter,
        List<Action<TestRun>> assertions,
        [CallerArgumentExpression(nameof(assertions))] string assertionExpression = "")
    {
        return RunTestsWithFilter(filter, assertions, new RunOptions(), "assertionExpression");
    }

    protected async Task RunTestsWithFilter(string filter,
        List<Action<TestRun>> assertions, RunOptions runOptions,
        [CallerArgumentExpression(nameof(assertions))] string assertionExpression = "")
    {
        {
            await RunWithoutAot(filter, assertions, runOptions, assertionExpression);
        }

        {
            await RunWithAot(filter, assertions, runOptions, assertionExpression);
        }

        {
            await RunWithSingleFile(filter, assertions, runOptions, assertionExpression);
        }
    }

    private static async Task RunWithoutAot(string filter,
        List<Action<TestRun>> assertions, RunOptions runOptions, string assertionExpression)
    {
        var trxFilename = Guid.NewGuid().ToString("N") + ".trx";

        var result = await Cli.Wrap("dotnet")
            .WithArguments(
                [
                    "run",
                    Sourcy.DotNet.Projects.TUnit_TestProject.FullName,
                    "--no-build",
                    "-f", "net8.0",
                    "--treenode-filter", filter,
                    "--report-trx", "--report-trx-filename", trxFilename,
                    // "--diagnostic", "--diagnostic-output-fileprefix", $"log_{GetType().Name}", 
                    "--timeout", "5m",
                    ..runOptions.AdditionalArguments
                ]
            )
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        await AssertTrx(result, assertions, trxFilename, assertionExpression);
    }
    
    private static async Task RunWithAot(string filter, List<Action<TestRun>> assertions,
        RunOptions runOptions, string assertionExpression)
    {
        var files = Sourcy.Git.RootDirectory
            .EnumerateDirectories("*", SearchOption.AllDirectories)
            .First(x => x.Name == "TESTPROJECT_AOT")
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .ToArray();

        var aotApp = files.FirstOrDefault(x => x.Name == "TUnit.TestProject") 
                     ?? files.First(x => x.Name == "TUnit.TestProject.exe");
        
        var trxFilename = Guid.NewGuid().ToString("N") + ".trx";
        
        var result = await Cli.Wrap(aotApp)
            .WithArguments(
                [
                    "--treenode-filter", filter,
                    "--report-trx", "--report-trx-filename", trxFilename,
                    // "--diagnostic", "--diagnostic-output-fileprefix", $"log_{GetType().Name}", 
                    "--timeout", "5m",
                    ..runOptions.AdditionalArguments
                ]
            )
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        await AssertTrx(result, assertions, trxFilename, assertionExpression);
    }
    
    private static async Task RunWithSingleFile(string filter,
        List<Action<TestRun>> assertions, RunOptions runOptions, string assertionExpression)
    {
        var files = Sourcy.Git.RootDirectory
            .EnumerateDirectories("*", SearchOption.AllDirectories)
            .First(x => x.Name == "TESTPROJECT_SINGLEFILE")
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .ToArray();
        
        var aotApp = files.FirstOrDefault(x => x.Name == "TUnit.TestProject") 
                     ?? files.First(x => x.Name == "TUnit.TestProject.exe");
        
        var trxFilename = Guid.NewGuid().ToString("N") + ".trx";
        
        var result = await Cli.Wrap(aotApp)
            .WithArguments(
                [
                    "--treenode-filter", filter,
                    "--report-trx", "--report-trx-filename", trxFilename,
                    // "--diagnostic", "--diagnostic-output-fileprefix", $"log_{GetType().Name}", 
                    "--timeout", "5m",
                    ..runOptions.AdditionalArguments
                ]
            )
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        await AssertTrx(result, assertions, trxFilename, assertionExpression);
    }

    protected FileInfo? FindFile(Func<FileInfo, bool> predicate)
    {
        return Sourcy.Git.RootDirectory
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .FirstOrDefault(predicate);
    }

    private static async Task AssertTrx(BufferedCommandResult commandResult,
        List<Action<TestRun>> assertions,
        string trxFilename, string assertionExpression)
    {
        try
        {
            var trxFileContents = await File.ReadAllTextAsync(
                Sourcy.Git.RootDirectory
                    .EnumerateFiles("*", SearchOption.AllDirectories)
                    .First(x => x.Name == trxFilename)
                    .FullName);

            var testRun = TrxControl.ReadTrx(new StringReader(trxFileContents));
            
            using (new AssertionScope())
            {
                assertions.ForEach(x => x.Invoke(testRun));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Command Input: {commandResult}");
            Console.WriteLine($"Error: {commandResult.StandardError}");
            Console.WriteLine($"Output: {commandResult.StandardOutput}");

            throw new Exception($"""
                                 Error asserting results

                                 Expression: {assertionExpression}
                                 """, e);
        }
    }
}

public record RunOptions
{
    public List<string> AdditionalArguments { get; init; } = [];
}
