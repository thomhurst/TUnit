// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace TUnit.Assertions.Assertions.Chars;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
#if NET
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsAscii))]
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsAsciiDigit))]
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsAsciiLetterLower))]
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsAsciiLetterUpper))]
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsAsciiLetterOrDigit))]
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsAsciiHexDigit))]
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsAsciiHexDigitUpper))]
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsAsciiHexDigitLower))]
#endif
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsDigit))]
// [GenerateAssertion<char>(AssertionType.Is, nameof(char.IsBetween))]
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsLetter))]
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsWhiteSpace))]
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsUpper))]
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsLower))]
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsPunctuation))]
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsLetterOrDigit))]
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsControl))]
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsNumber))]
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsSeparator))]
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsSurrogate))]
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsSymbol))]
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsHighSurrogate))]
[GenerateAssertion<char>(AssertionType.Is, nameof(char.IsLowSurrogate))]
public static partial class CharIsExtensions {
    
}
