using System.Runtime.CompilerServices;
using System.Xml;
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
        var trxFile = await WaitForTrxFile(trxFilename)
                      ?? throw new FileNotFoundException($"Could not find trx file {trxFilename}");

        var trxFileContents = await ReadTrxWithRetry(trxFile);

        var testRun = TrxControl.ReadTrx(new StringReader(trxFileContents));

        assertions.ForEach(x => x.Invoke(testRun));
    }

    // MTP can finish writing the trx after the subprocess returns control, so the file
    // may be missing or being written when the test reads it. Bounded retries keep the
    // window small enough to still surface a real "test never produced a trx" failure.
    private static async Task<string?> WaitForTrxFile(string trxFilename)
    {
        int[] backoffsMs = [50, 100, 250, 500, 1000];

        foreach (var delay in backoffsMs)
        {
            var found = FileSystemHelpers.FindFile(x => x.Name == trxFilename)?.FullName;
            if (found is not null)
            {
                return found;
            }

            await Task.Delay(delay);
        }

        return FileSystemHelpers.FindFile(x => x.Name == trxFilename)?.FullName;
    }

    // FileShare.ReadWrite lets us read past a still-open MTP write handle on Windows.
    // Re-reads on XML parse failure since a partial flush would yield truncated XML.
    private static async Task<string> ReadTrxWithRetry(string path)
    {
        int[] backoffsMs = [0, 100, 250, 500];

        Exception? lastError = null;
        foreach (var delay in backoffsMs)
        {
            if (delay > 0)
            {
                await Task.Delay(delay);
            }

            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                var contents = await reader.ReadToEndAsync();

                // Cheap shape check — if the trx is mid-flush we'd see something that
                // doesn't even parse as XML. Trigger another retry instead of bubbling
                // a misleading parse error up to the assertion list.
                using var probeReader = new StringReader(contents);
                using var xmlReader = XmlReader.Create(probeReader);
                while (xmlReader.Read()) { }

                return contents;
            }
            catch (Exception ex) when (ex is IOException or XmlException)
            {
                lastError = ex;
            }
        }

        throw new IOException($"Failed to read trx file '{path}' after retries.", lastError);
    }
}
