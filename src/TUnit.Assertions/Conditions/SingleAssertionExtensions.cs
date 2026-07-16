using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for float (Single) type using [AssertionFrom&lt;float&gt;] attributes.
/// Each assertion wraps a static method from the float class for special numeric value checks.
/// </summary>
[AssertionFrom<float>(nameof(float.IsNaN), ExpectationMessage = "be NaN")]
[AssertionFrom<float>(nameof(float.IsNaN), CustomName = "IsNotNaN", NegateLogic = true, ExpectationMessage = "be NaN")]

[AssertionFrom<float>(nameof(float.IsInfinity), ExpectationMessage = "be infinity")]
[AssertionFrom<float>(nameof(float.IsInfinity), CustomName = "IsNotInfinity", NegateLogic = true, ExpectationMessage = "be infinity")]

[AssertionFrom<float>(nameof(float.IsPositiveInfinity), ExpectationMessage = "be positive infinity")]
[AssertionFrom<float>(nameof(float.IsPositiveInfinity), CustomName = "IsNotPositiveInfinity", NegateLogic = true, ExpectationMessage = "be positive infinity")]

[AssertionFrom<float>(nameof(float.IsNegativeInfinity), ExpectationMessage = "be negative infinity")]
[AssertionFrom<float>(nameof(float.IsNegativeInfinity), CustomName = "IsNotNegativeInfinity", NegateLogic = true, ExpectationMessage = "be negative infinity")]

#if NET5_0_OR_GREATER
[AssertionFrom<float>(nameof(float.IsFinite), ExpectationMessage = "be finite")]
[AssertionFrom<float>(nameof(float.IsFinite), CustomName = "IsNotFinite", NegateLogic = true, ExpectationMessage = "be finite")]

[AssertionFrom<float>(nameof(float.IsNormal), ExpectationMessage = "be normal")]
[AssertionFrom<float>(nameof(float.IsNormal), CustomName = "IsNotNormal", NegateLogic = true, ExpectationMessage = "be normal")]

[AssertionFrom<float>(nameof(float.IsSubnormal), ExpectationMessage = "be subnormal")]
[AssertionFrom<float>(nameof(float.IsSubnormal), CustomName = "IsNotSubnormal", NegateLogic = true, ExpectationMessage = "be subnormal")]
#endif
public static partial class SingleAssertionExtensions
{
}
