using System.Runtime.CompilerServices;
using CliWrap;
using CliWrap.Buffered;
using TrxTools.TrxParser;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class TrxAsserter
{
    public static async Task AssertTrx(TestMode testMode, Command command, BufferedCommandResult commandResult,
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
            ThreadSafeOutput.WriteMultipleLines(
                $@"Mode: {testMode}",
                @$"Command Input: {command}",
                @$"Error: {commandResult.StandardError}",
                @$"Output: {commandResult.StandardOutput}"
            );

            throw new Exception($"""
                                 Error asserting results for {TestContext.Current!.TestDetails.MethodMetadata.Class.Name}: {e.Message}

                                 Expression: {assertionExpression}
                                 """, e);
        }
    }
}
