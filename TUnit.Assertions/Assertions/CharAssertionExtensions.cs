using System;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

// Using the new generic syntax for cleaner, more type-safe declarations
[CreateAssertion<char>(nameof(char.IsDigit))]
[CreateAssertion<char>(nameof(char.IsDigit), CustomName = "IsNotDigit", NegateLogic = true)]
[CreateAssertion<char>(nameof(char.IsLetter))]
[CreateAssertion<char>(nameof(char.IsLetter), CustomName = "IsNotLetter", NegateLogic = true)]
[CreateAssertion<char>(nameof(char.IsLetterOrDigit))]
[CreateAssertion<char>(nameof(char.IsLetterOrDigit), CustomName = "IsNotLetterOrDigit", NegateLogic = true)]
[CreateAssertion<char>(nameof(char.IsLower))]
[CreateAssertion<char>(nameof(char.IsLower), CustomName = "IsNotLower", NegateLogic = true)]
[CreateAssertion<char>(nameof(char.IsUpper))]
[CreateAssertion<char>(nameof(char.IsUpper), CustomName = "IsNotUpper", NegateLogic = true)]
[CreateAssertion<char>(nameof(char.IsNumber))]
[CreateAssertion<char>(nameof(char.IsNumber), CustomName = "IsNotNumber", NegateLogic = true)]
[CreateAssertion<char>(nameof(char.IsPunctuation))]
[CreateAssertion<char>(nameof(char.IsPunctuation), CustomName = "IsNotPunctuation", NegateLogic = true)]
[CreateAssertion<char>(nameof(char.IsSeparator))]
[CreateAssertion<char>(nameof(char.IsSeparator), CustomName = "IsNotSeparator", NegateLogic = true)]
[CreateAssertion<char>(nameof(char.IsSymbol))]
[CreateAssertion<char>(nameof(char.IsSymbol), CustomName = "IsNotSymbol", NegateLogic = true)]
[CreateAssertion<char>(nameof(char.IsWhiteSpace))]
[CreateAssertion<char>(nameof(char.IsWhiteSpace), CustomName = "IsNotWhiteSpace", NegateLogic = true)]
[CreateAssertion<char>(nameof(char.IsControl))]
[CreateAssertion<char>(nameof(char.IsControl), CustomName = "IsNotControl", NegateLogic = true)]
[CreateAssertion<char>(nameof(char.IsHighSurrogate))]
[CreateAssertion<char>(nameof(char.IsHighSurrogate), CustomName = "IsNotHighSurrogate", NegateLogic = true)]
[CreateAssertion<char>(nameof(char.IsLowSurrogate))]
[CreateAssertion<char>(nameof(char.IsLowSurrogate), CustomName = "IsNotLowSurrogate", NegateLogic = true)]
[CreateAssertion<char>(nameof(char.IsSurrogate))]
[CreateAssertion<char>(nameof(char.IsSurrogate), CustomName = "IsNotSurrogate", NegateLogic = true)]
public static partial class CharAssertionExtensions;
