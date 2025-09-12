using System;
using TUnit.Assertions.Attributes;

namespace TestNamespace;

[CreateAssertion(typeof(char), nameof(char.IsDigit), AssertionType.Is | AssertionType.IsNot)]
public static partial class CharAssertions
{
}