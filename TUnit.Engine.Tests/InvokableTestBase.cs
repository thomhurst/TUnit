using System.Runtime.CompilerServices;
using CliWrap;
using CliWrap.Buffered;
using FluentAssertions.Execution;
using TrxTools.TrxParser;

namespace TUnit.Engine.Tests;

public abstract class InvokableTestBase
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
        await RunWithoutAot(filter, assertions, runOptions, assertionExpression);

        await RunWithAot(filter, assertions, runOptions, assertionExpression);

        await RunWithSingleFile(filter, assertions, runOptions, assertionExpression);
    }

    private static async Task RunWithoutAot(string filter,
        List<Action<TestRun>> assertions, RunOptions runOptions, string assertionExpression)
    {
        var testProject = FindFile(x => x.Name == "TUnit.TestProject.csproj")!;
        var trxFilename = Guid.NewGuid().ToString("N") + ".trx";
        var result = await Cli.Wrap("dotnet")
            .WithArguments(
                [
                    "run",
                    "--no-build",
                    "-f", "net9.0",
                    "--treenode-filter", filter,
                    "--report-trx", "--report-trx-filename", trxFilename,
                    // "--diagnostic", "--diagnostic-output-fileprefix", $"log_{GetType().Name}", 
                    "--timeout", "5m",
                    ..runOptions.AdditionalArguments
                ]
            )
            .WithWorkingDirectory(testProject.DirectoryName!)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();


        await AssertTrx(result, assertions, trxFilename, assertionExpression);
    }
    
    private static async Task RunWithAot(string filter, List<Action<TestRun>> assertions,
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
        
        var trxFilename = Guid.NewGuid().ToString("N") + ".trx";
        
        var result = await Cli.Wrap(aotApp.Name)
            .WithArguments(
                [
                    "--treenode-filter", filter,
                    "--report-trx", "--report-trx-filename", trxFilename,
                    // "--diagnostic", "--diagnostic-output-fileprefix", $"log_{GetType().Name}", 
                    "--timeout", "5m",
                    ..runOptions.AdditionalArguments
                ]
            )
            .WithWorkingDirectory(aotApp.DirectoryName!)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        await AssertTrx(result, assertions, trxFilename, assertionExpression);
    }
    
    private static async Task RunWithSingleFile(string filter,
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
        
        var trxFilename = Guid.NewGuid().ToString("N") + ".trx";
        
        var result = await Cli.Wrap(aotApp.Name)
            .WithArguments(
                [
                    "--treenode-filter", filter,
                    "--report-trx", "--report-trx-filename", trxFilename,
                    // "--diagnostic", "--diagnostic-output-fileprefix", $"log_{GetType().Name}", 
                    "--timeout", "5m",
                    ..runOptions.AdditionalArguments
                ]
            )
            .WithWorkingDirectory(aotApp.DirectoryName!)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        await AssertTrx(result, assertions, trxFilename, assertionExpression);
    }

    protected static FileInfo? FindFile(Func<FileInfo, bool> predicate)
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (!directory!.EnumerateDirectories().Any(x => x.Name == ".git"))
        {
            directory = directory.Parent;
        }
        
        return directory
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .FirstOrDefault(predicate);
    }
    
    protected static DirectoryInfo? FindFolder(Func<DirectoryInfo, bool> predicate)
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (!directory!.EnumerateDirectories().Any(x => x.Name == ".git"))
        {
            directory = directory.Parent;
        }
        
        return directory
            .EnumerateDirectories("*", SearchOption.AllDirectories)
            .FirstOrDefault(predicate);
    }

    private static async Task AssertTrx(BufferedCommandResult commandResult,
        List<Action<TestRun>> assertions,
        string trxFilename, string assertionExpression)
    {
        try
        {
            var trxFileContents = await File.ReadAllTextAsync(
                FindFile(x => x.Name == trxFilename)!.FullName
            );

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
