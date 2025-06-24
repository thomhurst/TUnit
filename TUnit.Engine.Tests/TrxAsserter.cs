using System.Runtime.CompilerServices;
using CliWrap;
using CliWrap.Buffered;
using TrxTools.TrxParser;

namespace TUnit.Engine.Tests;

public class TrxAsserter
{
    public static async Task AssertTrx(Command command, BufferedCommandResult commandResult,
        List<Action<TestRun>> assertions,
        string trxFilename, [CallerArgumentExpression("assertions")] string assertionExpression = "")
    {
        try
        {
            var trxFile = FileSystemHelpers.FindFile(x => x.Name == trxFilename)?.FullName ?? throw new FileNotFoundException($"Could not find trx file {trxFilename}");

            var trxFileContents = await File.ReadAllTextAsync(trxFile);

            var testRun = TrxControl.ReadTrx(new StringReader(trxFileContents));

            assertions.ForEach(x => x.Invoke(testRun));
        }
        catch (Exception e)
        {
            Console.WriteLine(@$"Command Input: {command}");
            Console.WriteLine(@$"Error: {commandResult.StandardError}");
            Console.WriteLine(@$"Output: {commandResult.StandardOutput}");

            throw new Exception($"""
                                 Error asserting results for {TestContext.Current!.TestDetails.ClassMetadata.Name}: {e.Message}

                                 Expression: {assertionExpression}
                                 """, e);
        }
    }
}
