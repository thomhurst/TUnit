using System;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Assertions.Extensions;

[CreateAssertion(typeof(string), nameof(string.StartsWith), AssertionType.Is | AssertionType.IsNot)]
[CreateAssertion(typeof(string), nameof(string.EndsWith), AssertionType.Is | AssertionType.IsNot)]
[CreateAssertion(typeof(string), nameof(string.Contains), AssertionType.Is | AssertionType.IsNot)]
public static partial class StringAssertionExtensions
{
}