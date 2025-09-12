using System;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Assertions.Extensions;

[CreateAssertion(typeof(char), nameof(char.IsDigit), AssertionType.Is | AssertionType.IsNot)]
[CreateAssertion(typeof(char), nameof(char.IsLetter), AssertionType.Is | AssertionType.IsNot)]
[CreateAssertion(typeof(char), nameof(char.IsLetterOrDigit), AssertionType.Is | AssertionType.IsNot)]
[CreateAssertion(typeof(char), nameof(char.IsLower), AssertionType.Is | AssertionType.IsNot)]
[CreateAssertion(typeof(char), nameof(char.IsUpper), AssertionType.Is | AssertionType.IsNot)]
[CreateAssertion(typeof(char), nameof(char.IsNumber), AssertionType.Is | AssertionType.IsNot)]
[CreateAssertion(typeof(char), nameof(char.IsPunctuation), AssertionType.Is | AssertionType.IsNot)]
[CreateAssertion(typeof(char), nameof(char.IsSeparator), AssertionType.Is | AssertionType.IsNot)]
[CreateAssertion(typeof(char), nameof(char.IsSymbol), AssertionType.Is | AssertionType.IsNot)]
[CreateAssertion(typeof(char), nameof(char.IsWhiteSpace), AssertionType.Is | AssertionType.IsNot)]
[CreateAssertion(typeof(char), nameof(char.IsControl), AssertionType.Is | AssertionType.IsNot)]
[CreateAssertion(typeof(char), nameof(char.IsHighSurrogate), AssertionType.Is | AssertionType.IsNot)]
[CreateAssertion(typeof(char), nameof(char.IsLowSurrogate), AssertionType.Is | AssertionType.IsNot)]
[CreateAssertion(typeof(char), nameof(char.IsSurrogate), AssertionType.Is | AssertionType.IsNot)]
public static partial class CharAssertionExtensions
{
}