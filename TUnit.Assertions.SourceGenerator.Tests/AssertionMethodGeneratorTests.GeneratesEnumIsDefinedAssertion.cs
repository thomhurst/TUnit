using System;
using TUnit.Assertions.Attributes;

namespace TestNamespace;

[CreateAssertion(typeof(Enum), typeof(Enum), nameof(Enum.IsDefined), AssertionType.Is | AssertionType.IsNot, RequiresGenericTypeParameter = true)]
public static partial class EnumAssertions
{
}