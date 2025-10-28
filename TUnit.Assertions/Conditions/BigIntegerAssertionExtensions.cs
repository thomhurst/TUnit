using System.Numerics;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for BigInteger type using [AssertionFrom&lt;BigInteger&gt;] attributes.
/// Each assertion wraps a property from the BigInteger structure for numeric checks.
/// </summary>
[AssertionFrom<BigInteger>(nameof(BigInteger.IsZero), ExpectationMessage = "be zero")]
[AssertionFrom<BigInteger>(nameof(BigInteger.IsZero), CustomName = "IsNotZero", NegateLogic = true, ExpectationMessage = "be zero")]

#if NET6_0_OR_GREATER
[AssertionFrom<BigInteger>(nameof(BigInteger.IsOne), ExpectationMessage = "be one")]
[AssertionFrom<BigInteger>(nameof(BigInteger.IsOne), CustomName = "IsNotOne", NegateLogic = true, ExpectationMessage = "be one")]
#endif

[AssertionFrom<BigInteger>(nameof(BigInteger.IsEven), ExpectationMessage = "be even")]
[AssertionFrom<BigInteger>(nameof(BigInteger.IsEven), CustomName = "IsNotEven", NegateLogic = true, ExpectationMessage = "be even")]

#if NET6_0_OR_GREATER
[AssertionFrom<BigInteger>(nameof(BigInteger.IsPowerOfTwo), ExpectationMessage = "be a power of two")]
[AssertionFrom<BigInteger>(nameof(BigInteger.IsPowerOfTwo), CustomName = "IsNotPowerOfTwo", NegateLogic = true, ExpectationMessage = "be a power of two")]
#endif
public static partial class BigIntegerAssertionExtensions
{
}
