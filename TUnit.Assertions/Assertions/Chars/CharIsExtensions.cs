// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace TUnit.Assertions.Assertions.Chars;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
#if NET
[GenerateIsAssertion<char>(nameof(char.IsAscii))]
[GenerateIsAssertion<char>(nameof(char.IsAsciiDigit))]
[GenerateIsAssertion<char>(nameof(char.IsAsciiLetterLower))]
[GenerateIsAssertion<char>(nameof(char.IsAsciiLetterUpper))]
[GenerateIsAssertion<char>(nameof(char.IsAsciiLetterOrDigit))]
[GenerateIsAssertion<char>(nameof(char.IsAsciiHexDigit))]
[GenerateIsAssertion<char>(nameof(char.IsAsciiHexDigitUpper))]
[GenerateIsAssertion<char>(nameof(char.IsAsciiHexDigitLower))]
#endif
[GenerateIsAssertion<char>(nameof(char.IsDigit))]
// [GenerateIsAssertion<char>(nameof(char.IsBetween))]
[GenerateIsAssertion<char>(nameof(char.IsLetter))]
[GenerateIsAssertion<char>(nameof(char.IsWhiteSpace))]
[GenerateIsAssertion<char>(nameof(char.IsUpper))]
[GenerateIsAssertion<char>(nameof(char.IsLower))]
[GenerateIsAssertion<char>(nameof(char.IsPunctuation))]
[GenerateIsAssertion<char>(nameof(char.IsLetterOrDigit))]
[GenerateIsAssertion<char>(nameof(char.IsControl))]
[GenerateIsAssertion<char>(nameof(char.IsNumber))]
[GenerateIsAssertion<char>(nameof(char.IsSeparator))]
[GenerateIsAssertion<char>(nameof(char.IsSurrogate))]
[GenerateIsAssertion<char>(nameof(char.IsSymbol))]
[GenerateIsAssertion<char>(nameof(char.IsHighSurrogate))]
[GenerateIsAssertion<char>(nameof(char.IsLowSurrogate))]
public static partial class CharIsExtensions {
    
}
