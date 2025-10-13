using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for TimeZoneInfo type using [AssertionFrom&lt;TimeZoneInfo&gt;] attributes.
/// Each assertion wraps a property from the TimeZoneInfo class.
/// </summary>
[AssertionFrom<TimeZoneInfo>(nameof(TimeZoneInfo.SupportsDaylightSavingTime), ExpectationMessage = "support daylight saving time")]
[AssertionFrom<TimeZoneInfo>(nameof(TimeZoneInfo.SupportsDaylightSavingTime), CustomName = "DoesNotSupportDaylightSavingTime", NegateLogic = true, ExpectationMessage = "support daylight saving time")]

#if NET6_0_OR_GREATER
[AssertionFrom<TimeZoneInfo>(nameof(TimeZoneInfo.HasIanaId), ExpectationMessage = "have an IANA ID")]
[AssertionFrom<TimeZoneInfo>(nameof(TimeZoneInfo.HasIanaId), CustomName = "DoesNotHaveIanaId", NegateLogic = true, ExpectationMessage = "have an IANA ID")]
#endif
public static partial class TimeZoneInfoAssertionExtensions
{
}
