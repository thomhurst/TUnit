using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

#if NET5_0_OR_GREATER
using System.Text;

/// <summary>
/// Source-generated assertions for Rune type using [AssertionFrom&lt;Rune&gt;] attributes.
/// Each assertion wraps a static method or property from the Rune structure for Unicode scalar checks.
/// Rune is the modern, correct type for handling Unicode scalar values.
/// Available in .NET 5.0+
/// </summary>
[AssertionFrom<Rune>(nameof(Rune.IsAscii), ExpectationMessage = "be ASCII")]
[AssertionFrom<Rune>(nameof(Rune.IsAscii), CustomName = "IsNotAscii", NegateLogic = true, ExpectationMessage = "be ASCII")]

[AssertionFrom<Rune>(nameof(Rune.IsBmp), ExpectationMessage = "be in the Basic Multilingual Plane")]
[AssertionFrom<Rune>(nameof(Rune.IsBmp), CustomName = "IsNotBmp", NegateLogic = true, ExpectationMessage = "be in the Basic Multilingual Plane")]

[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsLetter), ExpectationMessage = "be a letter")]
[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsLetter), CustomName = "IsNotLetter", NegateLogic = true, ExpectationMessage = "be a letter")]

[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsDigit), ExpectationMessage = "be a digit")]
[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsDigit), CustomName = "IsNotDigit", NegateLogic = true, ExpectationMessage = "be a digit")]

[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsWhiteSpace), ExpectationMessage = "be whitespace")]
[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsWhiteSpace), CustomName = "IsNotWhiteSpace", NegateLogic = true, ExpectationMessage = "be whitespace")]

[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsUpper), ExpectationMessage = "be uppercase")]
[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsUpper), CustomName = "IsNotUpper", NegateLogic = true, ExpectationMessage = "be uppercase")]

[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsLower), ExpectationMessage = "be lowercase")]
[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsLower), CustomName = "IsNotLower", NegateLogic = true, ExpectationMessage = "be lowercase")]

[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsControl), ExpectationMessage = "be a control character")]
[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsControl), CustomName = "IsNotControl", NegateLogic = true, ExpectationMessage = "be a control character")]

[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsPunctuation), ExpectationMessage = "be punctuation")]
[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsPunctuation), CustomName = "IsNotPunctuation", NegateLogic = true, ExpectationMessage = "be punctuation")]

[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsSymbol), ExpectationMessage = "be a symbol")]
[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsSymbol), CustomName = "IsNotSymbol", NegateLogic = true, ExpectationMessage = "be a symbol")]

[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsNumber), ExpectationMessage = "be a number")]
[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsNumber), CustomName = "IsNotNumber", NegateLogic = true, ExpectationMessage = "be a number")]

[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsSeparator), ExpectationMessage = "be a separator")]
[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsSeparator), CustomName = "IsNotSeparator", NegateLogic = true, ExpectationMessage = "be a separator")]

[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsLetterOrDigit), ExpectationMessage = "be a letter or digit")]
[AssertionFrom<Rune>(typeof(Rune), nameof(Rune.IsLetterOrDigit), CustomName = "IsNotLetterOrDigit", NegateLogic = true, ExpectationMessage = "be a letter or digit")]
public static partial class RuneAssertionExtensions
{
}
#endif
