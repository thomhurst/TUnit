using System;
using TUnit.Assertions.Attributes;

namespace TestNamespace;

// Test with char.IsDigit(string s, int index) which has 2 parameters
[CreateAssertion(typeof(string), typeof(char), nameof(char.IsDigit), AssertionType.Is)]
public static partial class StringCharIndexAssertions
{
}