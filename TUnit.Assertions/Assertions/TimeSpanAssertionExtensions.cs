using System;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

[CreateAssertion(typeof(TimeSpan), typeof(TimeSpanAssertionExtensions), nameof(IsPositive))]
[CreateAssertion(typeof(TimeSpan), typeof(TimeSpanAssertionExtensions), nameof(IsPositive), CustomName = "IsNegativeOrZero", NegateLogic = true)]

[CreateAssertion(typeof(TimeSpan), typeof(TimeSpanAssertionExtensions), nameof(IsNegative))]
[CreateAssertion(typeof(TimeSpan), typeof(TimeSpanAssertionExtensions), nameof(IsNegative), CustomName = "IsPositiveOrZero", NegateLogic = true)]

[CreateAssertion(typeof(TimeSpan), typeof(TimeSpanAssertionExtensions), nameof(IsZero))]
[CreateAssertion(typeof(TimeSpan), typeof(TimeSpanAssertionExtensions), nameof(IsZero), CustomName = "IsNotZero", NegateLogic = true)]
public static partial class TimeSpanAssertionExtensions
{
    internal static bool IsPositive(TimeSpan timeSpan) => timeSpan > TimeSpan.Zero;
    internal static bool IsNegative(TimeSpan timeSpan) => timeSpan < TimeSpan.Zero;
    internal static bool IsZero(TimeSpan timeSpan) => timeSpan == TimeSpan.Zero;
}