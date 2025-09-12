using System;
using TUnit.Assertions.Attributes;

namespace TestNamespace;

// Test with Path.IsPathRooted(string path) which is a simple single-parameter method on a different class
[CreateAssertion(typeof(string), typeof(System.IO.Path), nameof(System.IO.Path.IsPathRooted), AssertionType.Is)]
public static partial class StringPathAssertions
{
}