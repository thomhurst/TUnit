namespace TUnit.Core.SourceGenerator.Tests;

public class CodeWriterTests
{
    [Test]
    [Arguments("", "")]
    [Arguments(" \t\r\n\u00a0\r\u2003\n", "")]
    [Arguments("first", "    first\n")]
    [Arguments("first\nsecond", "    first\n    second\n")]
    [Arguments("first\rsecond", "    first\n    second\n")]
    [Arguments("first\r\nsecond", "    first\n    second\n")]
    [Arguments("\r\n \t\n  first \t\r\n\r\n\t\rsecond\n \t\n", "      first\n\n\n    second\n")]
    [Arguments("first\u00a0\u2003\r\n\u00a0\nsecond\u0085", "    first\n\n    second\n")]
    [Arguments("first\r\r\nsecond\n\n", "    first\n\n    second\n")]
    [Arguments("first\u2028middle\nsecond", "    first\u2028middle\n    second\n")]
    public async Task AppendRaw_PreservesFormatting(string input, string expected)
    {
        var writer = new CodeWriter(includeHeader: false);
        writer.Indent();
        writer.AppendRaw(input);
        writer.Append("tail");

        await Assert.That(writer.ToString()).IsEqualTo(expected.Replace("\n", Environment.NewLine) + "    tail");
    }

    [Test]
    public async Task AppendRaw_PreservesPartialLineAndSubsequentWrites()
    {
        var writer = new CodeWriter(indentString: "--", includeHeader: false);
        writer.Indent();
        writer.Append("prefix:");
        writer.AppendRaw("\n  \r\nfirst \t\r\n\t\n second\r\n\n");
        writer.AppendRaw("third\n");
        writer.Append("tail");

        await Assert.That(writer.ToString()).IsEqualTo(
            "--prefix:first\n\n-- second\n--third\n--tail".Replace("\n", Environment.NewLine));
    }

    [Test]
    public async Task AppendRaw_EmptyInputDoesNotEndPartialLine()
    {
        var writer = new CodeWriter(includeHeader: false);
        writer.Append("prefix:");
        writer.AppendRaw(null!);
        writer.AppendRaw("");
        writer.AppendRaw(" \t\r\n\u00a0\n");
        writer.Append("tail");

        await Assert.That(writer.ToString()).IsEqualTo("prefix:tail");
    }
}
