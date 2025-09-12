using System;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

// DayOfWeek specific assertions
[CreateAssertion(typeof(DayOfWeek), typeof(DayOfWeekAssertionExtensions), nameof(IsWeekend))]
[CreateAssertion(typeof(DayOfWeek), typeof(DayOfWeekAssertionExtensions), nameof(IsWeekend), CustomName = "IsWeekday", NegateLogic = true)]

[CreateAssertion(typeof(DayOfWeek), typeof(DayOfWeekAssertionExtensions), nameof(IsMonday))]
[CreateAssertion(typeof(DayOfWeek), typeof(DayOfWeekAssertionExtensions), nameof(IsMonday), CustomName = "IsNotMonday", NegateLogic = true)]

[CreateAssertion(typeof(DayOfWeek), typeof(DayOfWeekAssertionExtensions), nameof(IsFriday))]
[CreateAssertion(typeof(DayOfWeek), typeof(DayOfWeekAssertionExtensions), nameof(IsFriday), CustomName = "IsNotFriday", NegateLogic = true)]
public static partial class DayOfWeekAssertionExtensions
{
    internal static bool IsWeekend(DayOfWeek dayOfWeek) =>
        dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    internal static bool IsMonday(DayOfWeek dayOfWeek) => dayOfWeek == DayOfWeek.Monday;
    internal static bool IsFriday(DayOfWeek dayOfWeek) => dayOfWeek == DayOfWeek.Friday;
}
