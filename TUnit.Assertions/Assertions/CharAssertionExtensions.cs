using System;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

[CreateAssertion(typeof(char), nameof(char.IsDigit))]
[CreateAssertion(typeof(char), nameof(char.IsDigit), CustomName = "IsNotDigit", NegateLogic = true)]
[CreateAssertion(typeof(char), nameof(char.IsLetter))]
[CreateAssertion(typeof(char), nameof(char.IsLetter), CustomName = "IsNotLetter", NegateLogic = true)]
[CreateAssertion(typeof(char), nameof(char.IsLetterOrDigit))]
[CreateAssertion(typeof(char), nameof(char.IsLetterOrDigit), CustomName = "IsNotLetterOrDigit", NegateLogic = true)]
[CreateAssertion(typeof(char), nameof(char.IsLower))]
[CreateAssertion(typeof(char), nameof(char.IsLower), CustomName = "IsNotLower", NegateLogic = true)]
[CreateAssertion(typeof(char), nameof(char.IsUpper))]
[CreateAssertion(typeof(char), nameof(char.IsUpper), CustomName = "IsNotUpper", NegateLogic = true)]
[CreateAssertion(typeof(char), nameof(char.IsNumber))]
[CreateAssertion(typeof(char), nameof(char.IsNumber), CustomName = "IsNotNumber", NegateLogic = true)]
[CreateAssertion(typeof(char), nameof(char.IsPunctuation))]
[CreateAssertion(typeof(char), nameof(char.IsPunctuation), CustomName = "IsNotPunctuation", NegateLogic = true)]
[CreateAssertion(typeof(char), nameof(char.IsSeparator))]
[CreateAssertion(typeof(char), nameof(char.IsSeparator), CustomName = "IsNotSeparator", NegateLogic = true)]
[CreateAssertion(typeof(char), nameof(char.IsSymbol))]
[CreateAssertion(typeof(char), nameof(char.IsSymbol), CustomName = "IsNotSymbol", NegateLogic = true)]
[CreateAssertion(typeof(char), nameof(char.IsWhiteSpace))]
[CreateAssertion(typeof(char), nameof(char.IsWhiteSpace), CustomName = "IsNotWhiteSpace", NegateLogic = true)]
[CreateAssertion(typeof(char), nameof(char.IsControl))]
[CreateAssertion(typeof(char), nameof(char.IsControl), CustomName = "IsNotControl", NegateLogic = true)]
[CreateAssertion(typeof(char), nameof(char.IsHighSurrogate))]
[CreateAssertion(typeof(char), nameof(char.IsHighSurrogate), CustomName = "IsNotHighSurrogate", NegateLogic = true)]
[CreateAssertion(typeof(char), nameof(char.IsLowSurrogate))]
[CreateAssertion(typeof(char), nameof(char.IsLowSurrogate), CustomName = "IsNotLowSurrogate", NegateLogic = true)]
[CreateAssertion(typeof(char), nameof(char.IsSurrogate))]
[CreateAssertion(typeof(char), nameof(char.IsSurrogate), CustomName = "IsNotSurrogate", NegateLogic = true)]
public static partial class CharAssertionExtensions;
