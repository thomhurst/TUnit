using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for char type using [AssertionFrom&lt;char&gt;] attributes.
/// Each assertion wraps a static method from the char class.
/// </summary>
[AssertionFrom<char>(nameof(char.IsLetter), ExpectationMessage = "be a letter")]
[AssertionFrom<char>(nameof(char.IsLetter), CustomName = "IsNotLetter", NegateLogic = true, ExpectationMessage = "be a letter")]

[AssertionFrom<char>(nameof(char.IsDigit), ExpectationMessage = "be a digit")]
[AssertionFrom<char>(nameof(char.IsDigit), CustomName = "IsNotDigit", NegateLogic = true, ExpectationMessage = "be a digit")]

[AssertionFrom<char>(nameof(char.IsWhiteSpace), ExpectationMessage = "be whitespace")]
[AssertionFrom<char>(nameof(char.IsWhiteSpace), CustomName = "IsNotWhiteSpace", NegateLogic = true, ExpectationMessage = "be whitespace")]

[AssertionFrom<char>(nameof(char.IsUpper), ExpectationMessage = "be uppercase")]
[AssertionFrom<char>(nameof(char.IsUpper), CustomName = "IsNotUpper", NegateLogic = true, ExpectationMessage = "be uppercase")]

[AssertionFrom<char>(nameof(char.IsLower), ExpectationMessage = "be lowercase")]
[AssertionFrom<char>(nameof(char.IsLower), CustomName = "IsNotLower", NegateLogic = true, ExpectationMessage = "be lowercase")]

[AssertionFrom<char>(nameof(char.IsControl), ExpectationMessage = "be a control character")]
[AssertionFrom<char>(nameof(char.IsControl), CustomName = "IsNotControl", NegateLogic = true, ExpectationMessage = "be a control character")]

[AssertionFrom<char>(nameof(char.IsPunctuation), ExpectationMessage = "be punctuation")]
[AssertionFrom<char>(nameof(char.IsPunctuation), CustomName = "IsNotPunctuation", NegateLogic = true, ExpectationMessage = "be punctuation")]

[AssertionFrom<char>(nameof(char.IsSymbol), ExpectationMessage = "be a symbol")]
[AssertionFrom<char>(nameof(char.IsSymbol), CustomName = "IsNotSymbol", NegateLogic = true, ExpectationMessage = "be a symbol")]

[AssertionFrom<char>(nameof(char.IsNumber), ExpectationMessage = "be a number")]
[AssertionFrom<char>(nameof(char.IsNumber), CustomName = "IsNotNumber", NegateLogic = true, ExpectationMessage = "be a number")]

[AssertionFrom<char>(nameof(char.IsSeparator), ExpectationMessage = "be a separator")]
[AssertionFrom<char>(nameof(char.IsSeparator), CustomName = "IsNotSeparator", NegateLogic = true, ExpectationMessage = "be a separator")]

[AssertionFrom<char>(nameof(char.IsSurrogate), ExpectationMessage = "be a surrogate")]
[AssertionFrom<char>(nameof(char.IsSurrogate), CustomName = "IsNotSurrogate", NegateLogic = true, ExpectationMessage = "be a surrogate")]

[AssertionFrom<char>(nameof(char.IsHighSurrogate), ExpectationMessage = "be a high surrogate")]
[AssertionFrom<char>(nameof(char.IsHighSurrogate), CustomName = "IsNotHighSurrogate", NegateLogic = true, ExpectationMessage = "be a high surrogate")]

[AssertionFrom<char>(nameof(char.IsLowSurrogate), ExpectationMessage = "be a low surrogate")]
[AssertionFrom<char>(nameof(char.IsLowSurrogate), CustomName = "IsNotLowSurrogate", NegateLogic = true, ExpectationMessage = "be a low surrogate")]

[AssertionFrom<char>(nameof(char.IsLetterOrDigit), ExpectationMessage = "be a letter or digit")]
[AssertionFrom<char>(nameof(char.IsLetterOrDigit), CustomName = "IsNotLetterOrDigit", NegateLogic = true, ExpectationMessage = "be a letter or digit")]
public static partial class CharAssertionExtensions
{
}
