using TUnit.Assertions.Extensions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class CharAssertionTests
{
    [Test]
    public async Task Test_Char_IsLetter()
    {
        var value = 'A';
        await Assert.That(value).IsLetter();
    }

    [Test]
    public async Task Test_Char_IsLetter_Lowercase()
    {
        var value = 'z';
        await Assert.That(value).IsLetter();
    }

    [Test]
    public async Task Test_Char_IsNotLetter()
    {
        var value = '5';
        await Assert.That(value).IsNotLetter();
    }

    [Test]
    public async Task Test_Char_IsDigit()
    {
        var value = '7';
        await Assert.That(value).IsDigit();
    }

    [Test]
    public async Task Test_Char_IsDigit_Zero()
    {
        var value = '0';
        await Assert.That(value).IsDigit();
    }

    [Test]
    public async Task Test_Char_IsNotDigit()
    {
        var value = 'A';
        await Assert.That(value).IsNotDigit();
    }

    [Test]
    public async Task Test_Char_IsWhiteSpace_Space()
    {
        var value = ' ';
        await Assert.That(value).IsWhiteSpace();
    }

    [Test]
    public async Task Test_Char_IsWhiteSpace_Tab()
    {
        var value = '\t';
        await Assert.That(value).IsWhiteSpace();
    }

    [Test]
    public async Task Test_Char_IsWhiteSpace_Newline()
    {
        var value = '\n';
        await Assert.That(value).IsWhiteSpace();
    }

    [Test]
    public async Task Test_Char_IsNotWhiteSpace()
    {
        var value = 'A';
        await Assert.That(value).IsNotWhiteSpace();
    }

    [Test]
    public async Task Test_Char_IsUpper()
    {
        var value = 'Z';
        await Assert.That(value).IsUpper();
    }

    [Test]
    public async Task Test_Char_IsNotUpper()
    {
        var value = 'a';
        await Assert.That(value).IsNotUpper();
    }

    [Test]
    public async Task Test_Char_IsLower()
    {
        var value = 'b';
        await Assert.That(value).IsLower();
    }

    [Test]
    public async Task Test_Char_IsNotLower()
    {
        var value = 'B';
        await Assert.That(value).IsNotLower();
    }

    [Test]
    public async Task Test_Char_IsControl()
    {
        var value = '\u0001'; // Start of heading control character
        await Assert.That(value).IsControl();
    }

    [Test]
    public async Task Test_Char_IsControl_Newline()
    {
        var value = '\n';
        await Assert.That(value).IsControl();
    }

    [Test]
    public async Task Test_Char_IsNotControl()
    {
        var value = 'A';
        await Assert.That(value).IsNotControl();
    }

    [Test]
    public async Task Test_Char_IsPunctuation_Period()
    {
        var value = '.';
        await Assert.That(value).IsPunctuation();
    }

    [Test]
    public async Task Test_Char_IsPunctuation_Comma()
    {
        var value = ',';
        await Assert.That(value).IsPunctuation();
    }

    [Test]
    public async Task Test_Char_IsPunctuation_ExclamationMark()
    {
        var value = '!';
        await Assert.That(value).IsPunctuation();
    }

    [Test]
    public async Task Test_Char_IsNotPunctuation()
    {
        var value = 'A';
        await Assert.That(value).IsNotPunctuation();
    }

    [Test]
    public async Task Test_Char_IsSymbol_Plus()
    {
        var value = '+';
        await Assert.That(value).IsSymbol();
    }

    [Test]
    public async Task Test_Char_IsSymbol_Dollar()
    {
        var value = '$';
        await Assert.That(value).IsSymbol();
    }

    [Test]
    public async Task Test_Char_IsNotSymbol()
    {
        var value = 'A';
        await Assert.That(value).IsNotSymbol();
    }

    [Test]
    public async Task Test_Char_IsNumber()
    {
        var value = '8';
        await Assert.That(value).IsNumber();
    }

    [Test]
    public async Task Test_Char_IsNotNumber()
    {
        var value = 'A';
        await Assert.That(value).IsNotNumber();
    }

    [Test]
    public async Task Test_Char_IsSeparator()
    {
        var value = ' '; // Space is a separator
        await Assert.That(value).IsSeparator();
    }

    [Test]
    public async Task Test_Char_IsNotSeparator()
    {
        var value = 'A';
        await Assert.That(value).IsNotSeparator();
    }

    [Test]
    public async Task Test_Char_IsSurrogate()
    {
        var value = '\uD800'; // High surrogate
        await Assert.That(value).IsSurrogate();
    }

    [Test]
    public async Task Test_Char_IsNotSurrogate()
    {
        var value = 'A';
        await Assert.That(value).IsNotSurrogate();
    }

    [Test]
    public async Task Test_Char_IsHighSurrogate()
    {
        var value = '\uD800'; // High surrogate
        await Assert.That(value).IsHighSurrogate();
    }

    [Test]
    public async Task Test_Char_IsNotHighSurrogate()
    {
        var value = 'A';
        await Assert.That(value).IsNotHighSurrogate();
    }

    [Test]
    public async Task Test_Char_IsLowSurrogate()
    {
        var value = '\uDC00'; // Low surrogate
        await Assert.That(value).IsLowSurrogate();
    }

    [Test]
    public async Task Test_Char_IsNotLowSurrogate()
    {
        var value = 'A';
        await Assert.That(value).IsNotLowSurrogate();
    }

    [Test]
    public async Task Test_Char_IsLetterOrDigit_Letter()
    {
        var value = 'A';
        await Assert.That(value).IsLetterOrDigit();
    }

    [Test]
    public async Task Test_Char_IsLetterOrDigit_Digit()
    {
        var value = '5';
        await Assert.That(value).IsLetterOrDigit();
    }

    [Test]
    public async Task Test_Char_IsNotLetterOrDigit()
    {
        var value = '@';
        await Assert.That(value).IsNotLetterOrDigit();
    }
}
