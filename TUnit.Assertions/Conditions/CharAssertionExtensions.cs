using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for char type using [AssertionFrom&lt;char&gt;] attributes.
/// Each assertion wraps a static method from the char class.
/// </summary>
[AssertionFrom<char>("IsLetter", ExpectationMessage = "be a letter")]
[AssertionFrom<char>("IsLetter", CustomName = "IsNotLetter", NegateLogic = true, ExpectationMessage = "be a letter")]

[AssertionFrom<char>("IsDigit", ExpectationMessage = "be a digit")]
[AssertionFrom<char>("IsDigit", CustomName = "IsNotDigit", NegateLogic = true, ExpectationMessage = "be a digit")]

[AssertionFrom<char>("IsWhiteSpace", ExpectationMessage = "be whitespace")]
[AssertionFrom<char>("IsWhiteSpace", CustomName = "IsNotWhiteSpace", NegateLogic = true, ExpectationMessage = "be whitespace")]

[AssertionFrom<char>("IsUpper", ExpectationMessage = "be uppercase")]
[AssertionFrom<char>("IsUpper", CustomName = "IsNotUpper", NegateLogic = true, ExpectationMessage = "be uppercase")]

[AssertionFrom<char>("IsLower", ExpectationMessage = "be lowercase")]
[AssertionFrom<char>("IsLower", CustomName = "IsNotLower", NegateLogic = true, ExpectationMessage = "be lowercase")]

[AssertionFrom<char>("IsControl", ExpectationMessage = "be a control character")]
[AssertionFrom<char>("IsControl", CustomName = "IsNotControl", NegateLogic = true, ExpectationMessage = "be a control character")]

[AssertionFrom<char>("IsPunctuation", ExpectationMessage = "be punctuation")]
[AssertionFrom<char>("IsPunctuation", CustomName = "IsNotPunctuation", NegateLogic = true, ExpectationMessage = "be punctuation")]

[AssertionFrom<char>("IsSymbol", ExpectationMessage = "be a symbol")]
[AssertionFrom<char>("IsSymbol", CustomName = "IsNotSymbol", NegateLogic = true, ExpectationMessage = "be a symbol")]

[AssertionFrom<char>("IsNumber", ExpectationMessage = "be a number")]
[AssertionFrom<char>("IsNumber", CustomName = "IsNotNumber", NegateLogic = true, ExpectationMessage = "be a number")]

[AssertionFrom<char>("IsSeparator", ExpectationMessage = "be a separator")]
[AssertionFrom<char>("IsSeparator", CustomName = "IsNotSeparator", NegateLogic = true, ExpectationMessage = "be a separator")]

[AssertionFrom<char>("IsSurrogate", ExpectationMessage = "be a surrogate")]
[AssertionFrom<char>("IsSurrogate", CustomName = "IsNotSurrogate", NegateLogic = true, ExpectationMessage = "be a surrogate")]

[AssertionFrom<char>("IsHighSurrogate", ExpectationMessage = "be a high surrogate")]
[AssertionFrom<char>("IsHighSurrogate", CustomName = "IsNotHighSurrogate", NegateLogic = true, ExpectationMessage = "be a high surrogate")]

[AssertionFrom<char>("IsLowSurrogate", ExpectationMessage = "be a low surrogate")]
[AssertionFrom<char>("IsLowSurrogate", CustomName = "IsNotLowSurrogate", NegateLogic = true, ExpectationMessage = "be a low surrogate")]

[AssertionFrom<char>("IsLetterOrDigit", ExpectationMessage = "be a letter or digit")]
[AssertionFrom<char>("IsLetterOrDigit", CustomName = "IsNotLetterOrDigit", NegateLogic = true, ExpectationMessage = "be a letter or digit")]
public static partial class CharAssertionExtensions
{
}
