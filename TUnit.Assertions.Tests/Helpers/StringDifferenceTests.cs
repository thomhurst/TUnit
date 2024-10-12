﻿namespace TUnit.Assertions.Tests.AssertConditions;

public class StringDifferenceTests
{
    [Test]
    public async Task Works_For_Empty_String_As_Actual()
    {
        string expectedMessage = """
                                 Expected actual to be equal to "some text", but found "" which differs at index 0:
                                     ↓
                                    ""
                                    "some text"
                                     ↑.
                                 At Assert.That(actual).IsEqualTo(expected, StringComparison.Ordinal)
                                 """;
        var actual = "";
        var expected = "some text";

        var sut = async ()
            => await Assert.That(actual).IsEqualTo(expected);

        await Assert.That(sut).ThrowsException()
            .WithMessage(expectedMessage);
    }

    [Test]
    public async Task Works_For_Empty_String_As_Expected()
    {
        string expectedMessage = """
                                 Expected actual to be equal to "", but found "actual text" which differs at index 0:
                                     ↓
                                    "actual text"
                                    ""
                                     ↑.
                                 At Assert.That(actual).IsEqualTo(expected, StringComparison.Ordinal)
                                 """;
        var actual = "actual text";
        var expected = "";

        var sut = async ()
            => await Assert.That(actual).IsEqualTo(expected);

        await Assert.That(sut).ThrowsException()
            .WithMessage(expectedMessage);
    }

    [Test]
    public async Task Works_When_Actual_Starts_With_Expected()
    {
        string expectedMessage = """
                                 Expected actual to be equal to "some text", but found "some" which differs at index 4:
                                         ↓
                                    "some"
                                    "some text"
                                         ↑.
                                 At Assert.That(actual).IsEqualTo(expected, StringComparison.Ordinal)
                                 """;
        var actual = "some";
        var expected = "some text";

        var sut = async ()
            => await Assert.That(actual).IsEqualTo(expected);

        await Assert.That(sut).ThrowsException()
            .WithMessage(expectedMessage);
    }

    [Test]
    public async Task Works_When_Expected_Starts_With_Actual()
    {
        string expectedMessage = """
                                 Expected actual to be equal to "some", but found "some text" which differs at index 4:
                                         ↓
                                    "some text"
                                    "some"
                                         ↑.
                                 At Assert.That(actual).IsEqualTo(expected, StringComparison.Ordinal)
                                 """;
        var actual = "some text";
        var expected = "some";

        var sut = async ()
            => await Assert.That(actual).IsEqualTo(expected);

        await Assert.That(sut).ThrowsException()
            .WithMessage(expectedMessage);
    }
}