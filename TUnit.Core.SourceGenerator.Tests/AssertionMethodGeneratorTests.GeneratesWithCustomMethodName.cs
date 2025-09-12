using System;
using TUnit.Assertions.Attributes;

namespace TestNamespace;

[CreateAssertion(typeof(char), nameof(char.IsDigit), AssertionType.Is, CustomMethodName = "IsNumeric")]
public static partial class CharAssertions
{
}