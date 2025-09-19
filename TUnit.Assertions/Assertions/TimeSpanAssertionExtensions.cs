using System;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

[CreateAssertion<TimeSpan>( typeof(TimeSpanAssertionExtensions), nameof(IsPositive))]
[CreateAssertion<TimeSpan>( typeof(TimeSpanAssertionExtensions), nameof(IsPositive), CustomName = "IsNegativeOrZero", NegateLogic = true)]

[CreateAssertion<TimeSpan>( typeof(TimeSpanAssertionExtensions), nameof(IsNegative))]
[CreateAssertion<TimeSpan>( typeof(TimeSpanAssertionExtensions), nameof(IsNegative), CustomName = "IsPositiveOrZero", NegateLogic = true)]

[CreateAssertion<TimeSpan>( typeof(TimeSpanAssertionExtensions), nameof(IsZero))]
[CreateAssertion<TimeSpan>( typeof(TimeSpanAssertionExtensions), nameof(IsZero), CustomName = "IsNotZero", NegateLogic = true)]
public static partial class TimeSpanAssertionExtensions
{
    internal static bool IsPositive(TimeSpan timeSpan) => timeSpan > TimeSpan.Zero;
    internal static bool IsNegative(TimeSpan timeSpan) => timeSpan < TimeSpan.Zero;
    internal static bool IsZero(TimeSpan timeSpan) => timeSpan == TimeSpan.Zero;
}