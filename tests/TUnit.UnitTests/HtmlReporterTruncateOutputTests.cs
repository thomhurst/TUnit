using TUnit.Engine.Reporters.Html;

namespace TUnit.UnitTests;

public class HtmlReporterTruncateOutputTests
{
    [Test]
    public async Task TruncateOutput_Null_ReturnsNull()
    {
        var result = HtmlReporter.TruncateOutput(null);
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task TruncateOutput_EmptyString_ReturnsEmpty()
    {
        var result = HtmlReporter.TruncateOutput(string.Empty);
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task TruncateOutput_ShortString_ReturnedAsIs()
    {
        var input = "short output";
        var result = HtmlReporter.TruncateOutput(input);
        await Assert.That(result).IsEqualTo(input);
    }

    [Test]
    public async Task TruncateOutput_AtExactLimit_ReturnedAsIs()
    {
        var input = new string('a', HtmlReporter.MaxOutputLength);
        var result = HtmlReporter.TruncateOutput(input);
        await Assert.That(result).IsEqualTo(input);
    }

    [Test]
    public async Task TruncateOutput_OneCharOverLimit_TruncatedWithNote()
    {
        var input = new string('a', HtmlReporter.MaxOutputLength + 1);
        var result = HtmlReporter.TruncateOutput(input);
        await Assert.That(result).IsNotNull();
        // The truncated content before the note must be exactly MaxOutputLength chars.
        var noteIndex = result!.IndexOf('\n');
        await Assert.That(noteIndex).IsEqualTo(HtmlReporter.MaxOutputLength);
        await Assert.That(result).Contains("output truncated");
        await Assert.That(result).Contains((HtmlReporter.MaxOutputLength + 1).ToString("N0"));
    }

    [Test]
    public async Task TruncateOutput_LargeString_TruncatedWithNote()
    {
        var input = new string('x', HtmlReporter.MaxOutputLength * 2);
        var result = HtmlReporter.TruncateOutput(input);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!).StartsWith(new string('x', HtmlReporter.MaxOutputLength));
        await Assert.That(result).Contains("output truncated");
    }

    [Test]
    public async Task TruncateOutput_SurrogatePairAtBoundary_DoesNotSplitPair()
    {
        // Build a string where the char at MaxOutputLength-1 is a high surrogate
        // by placing a surrogate pair straddling the cut point.
        var prefix = new string('a', HtmlReporter.MaxOutputLength - 1);
        // U+1F600 (😀) is encoded as a surrogate pair: \uD83D\uDE00
        const string surrogateEmoji = "\uD83D\uDE00";
        var input = prefix + surrogateEmoji + new string('b', 10);

        var result = HtmlReporter.TruncateOutput(input);

        await Assert.That(result).IsNotNull();
        // The high surrogate at MaxOutputLength-1 should cause the cut to back off by 1,
        // so the result must not end with an unpaired surrogate.
        var truncatedPart = result![..result.IndexOf('\n')];
        await Assert.That(char.IsHighSurrogate(truncatedPart[^1])).IsFalse();
    }
}
