// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace TUnit.Assertions.Assertions.Chars;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
#if NET
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsAscii))]
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsAsciiDigit))]
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsAsciiLetterLower))]
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsAsciiLetterUpper))]
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsAsciiLetterOrDigit))]
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsAsciiHexDigit))]
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsAsciiHexDigitUpper))]
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsAsciiHexDigitLower))]
#endif
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsDigit))]
// [GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsBetween))]
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsLetter))]
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsWhiteSpace))]
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsUpper))]
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsLower))]
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsPunctuation))]
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsLetterOrDigit))]
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsControl))]
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsNumber))]
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsSeparator))]
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsSurrogate))]
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsSymbol))]
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsHighSurrogate))]
[GenerateAssertion<char>(AssertionType.IsNot, nameof(char.IsLowSurrogate))]
public static partial class CharIsNotExtensions {
    
}
