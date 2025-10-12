using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for DateTime type using [AssertionFrom&lt;DateTime&gt;] attributes.
/// These wrap instance methods from the DateTime struct.
/// </summary>
[AssertionFrom<DateTime>("IsDaylightSavingTime", ExpectationMessage = "be during daylight saving time")]
[AssertionFrom<DateTime>("IsDaylightSavingTime", CustomName = "IsNotDaylightSavingTime", NegateLogic = true, ExpectationMessage = "be during daylight saving time")]
public static partial class DateTimeMethodAssertions
{
}
