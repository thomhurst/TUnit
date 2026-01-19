namespace TUnit.Assertions.Tests.Helpers;

public class StringDifferenceTests
{
    [Test]
    public async Task Works_For_Empty_String_As_Actual()
    {
        var expectedMessage = """
                              Expected to be equal to "some text"
                              but found ""

                              at Assert.That(actual).IsEqualTo(expected)
                              """.NormalizeLineEndings();
        var actual = "";
        var expected = "some text";

        var sut = async ()
            => await Assert.That(actual).IsEqualTo(expected);

        var exception = await Assert.That(sut).ThrowsException();
        await Assert.That(exception.Message.NormalizeLineEndings()).IsEqualTo(expectedMessage);
    }

    [Test]
    public async Task Works_For_Empty_String_As_Expected()
    {
        var expectedMessage = """
                              Expected to be equal to ""
                              but found "actual text"

                              at Assert.That(actual).IsEqualTo(expected)
                              """.NormalizeLineEndings();
        var actual = "actual text";
        var expected = "";

        var sut = async ()
            => await Assert.That(actual).IsEqualTo(expected);

        var exception = await Assert.That(sut).ThrowsException();
        await Assert.That(exception.Message.NormalizeLineEndings()).IsEqualTo(expectedMessage);
    }

    [Test]
    public async Task Works_When_Actual_Starts_With_Expected()
    {
        var expectedMessage = """
                              Expected to be equal to "some text"
                              but found "some"

                              at Assert.That(actual).IsEqualTo(expected)
                              """.NormalizeLineEndings();
        var actual = "some";
        var expected = "some text";

        var sut = async ()
            => await Assert.That(actual).IsEqualTo(expected);

        var exception = await Assert.That(sut).ThrowsException();
        await Assert.That(exception.Message.NormalizeLineEndings()).IsEqualTo(expectedMessage);
    }

    [Test]
    public async Task Works_When_Expected_Starts_With_Actual()
    {
        var expectedMessage = """
                              Expected to be equal to "some"
                              but found "some text"

                              at Assert.That(actual).IsEqualTo(expected)
                              """.NormalizeLineEndings();
        var actual = "some text";
        var expected = "some";

        var sut = async ()
            => await Assert.That(actual).IsEqualTo(expected);

        var exception = await Assert.That(sut).ThrowsException();
        await Assert.That(exception.Message.NormalizeLineEndings()).IsEqualTo(expectedMessage);
    }

    [Test]
    public async Task Multiline_String_Difference_Shows_Line_Diff()
    {
        var expected = "line 1\nline 2\nline 3";
        var actual = "line 1\nchanged\nline 3";

        var sut = async () => await Assert.That(actual).IsEqualTo(expected);

        var exception = await Assert.That(sut).ThrowsException();
        await Assert.That(exception.Message.NormalizeLineEndings())
            .Contains("differences starting at line 2")
            .And.Contains("- 2: line 2")
            .And.Contains("+ 2: changed");
    }

    [Test]
    public async Task Multiline_String_Shows_Context_Lines()
    {
        var expected = "line 1\nline 2\nline 3\nline 4\nline 5";
        var actual = "line 1\nline 2\nchanged\nline 4\nline 5";

        var sut = async () => await Assert.That(actual).IsEqualTo(expected);

        var exception = await Assert.That(sut).ThrowsException();
        // Should show context lines around the difference
        await Assert.That(exception.Message.NormalizeLineEndings())
            .Contains("1: line 1")
            .And.Contains("2: line 2")
            .And.Contains("- 3: line 3")
            .And.Contains("+ 3: changed")
            .And.Contains("4: line 4")
            .And.Contains("5: line 5");
    }

    [Test]
    public async Task Multiline_String_Handles_Added_Lines()
    {
        var expected = "line 1\nline 2";
        var actual = "line 1\nline 2\nline 3";

        var sut = async () => await Assert.That(actual).IsEqualTo(expected);

        var exception = await Assert.That(sut).ThrowsException();
        await Assert.That(exception.Message.NormalizeLineEndings())
            .Contains("+ 3: line 3");
    }

    [Test]
    public async Task Multiline_String_Handles_Removed_Lines()
    {
        var expected = "line 1\nline 2\nline 3";
        var actual = "line 1\nline 2";

        var sut = async () => await Assert.That(actual).IsEqualTo(expected);

        var exception = await Assert.That(sut).ThrowsException();
        await Assert.That(exception.Message.NormalizeLineEndings())
            .Contains("- 3: line 3");
    }

    [Test]
    public async Task Multiline_String_With_CRLF_Line_Endings()
    {
        var expected = "line 1\r\nline 2\r\nline 3";
        var actual = "line 1\r\nchanged\r\nline 3";

        var sut = async () => await Assert.That(actual).IsEqualTo(expected);

        var exception = await Assert.That(sut).ThrowsException();
        await Assert.That(exception.Message.NormalizeLineEndings())
            .Contains("differences starting at line 2");
    }

    [Test]
    public async Task Multiline_String_With_Case_Insensitive_Comparison()
    {
        var expected = "LINE 1\nline 2";
        var actual = "line 1\nLINE 2";

        // Should be equal with case-insensitive comparison
        await Assert.That(actual).IsEqualTo(expected).IgnoringCase();
    }
}
