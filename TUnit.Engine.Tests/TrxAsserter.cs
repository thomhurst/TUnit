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
        var trxFile = FileSystemHelpers.FindFile(x => x.Name == trxFilename)?.FullName ?? throw new FileNotFoundException($"Could not find trx file {trxFilename}");

        var trxFileContents = await FilePolyfill.ReadAllTextAsync(trxFile);

        var testRun = TrxControl.ReadTrx(new StringReader(trxFileContents));

        assertions.ForEach(x => x.Invoke(testRun));
    }
}
