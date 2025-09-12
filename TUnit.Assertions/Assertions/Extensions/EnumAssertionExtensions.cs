using System;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Assertions.Extensions;

[CreateAssertion(typeof(Enum), typeof(Enum), nameof(Enum.IsDefined), AssertionType.Is | AssertionType.IsNot, RequiresGenericTypeParameter = true)]
public static partial class EnumAssertionExtensions
{
}