using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for double type using [AssertionFrom&lt;double&gt;] attributes.
/// Each assertion wraps a static method from the double class for special numeric value checks.
/// </summary>
[AssertionFrom<double>(nameof(double.IsNaN), ExpectationMessage = "be NaN")]
[AssertionFrom<double>(nameof(double.IsNaN), CustomName = "IsNotNaN", NegateLogic = true, ExpectationMessage = "be NaN")]

[AssertionFrom<double>(nameof(double.IsInfinity), ExpectationMessage = "be infinity")]
[AssertionFrom<double>(nameof(double.IsInfinity), CustomName = "IsNotInfinity", NegateLogic = true, ExpectationMessage = "be infinity")]

[AssertionFrom<double>(nameof(double.IsPositiveInfinity), ExpectationMessage = "be positive infinity")]
[AssertionFrom<double>(nameof(double.IsPositiveInfinity), CustomName = "IsNotPositiveInfinity", NegateLogic = true, ExpectationMessage = "be positive infinity")]

[AssertionFrom<double>(nameof(double.IsNegativeInfinity), ExpectationMessage = "be negative infinity")]
[AssertionFrom<double>(nameof(double.IsNegativeInfinity), CustomName = "IsNotNegativeInfinity", NegateLogic = true, ExpectationMessage = "be negative infinity")]

#if NET5_0_OR_GREATER
[AssertionFrom<double>(nameof(double.IsFinite), ExpectationMessage = "be finite")]
[AssertionFrom<double>(nameof(double.IsFinite), CustomName = "IsNotFinite", NegateLogic = true, ExpectationMessage = "be finite")]

[AssertionFrom<double>(nameof(double.IsNormal), ExpectationMessage = "be normal")]
[AssertionFrom<double>(nameof(double.IsNormal), CustomName = "IsNotNormal", NegateLogic = true, ExpectationMessage = "be normal")]

[AssertionFrom<double>(nameof(double.IsSubnormal), ExpectationMessage = "be subnormal")]
[AssertionFrom<double>(nameof(double.IsSubnormal), CustomName = "IsNotSubnormal", NegateLogic = true, ExpectationMessage = "be subnormal")]
#endif
public static partial class DoubleAssertionExtensions
{
}
