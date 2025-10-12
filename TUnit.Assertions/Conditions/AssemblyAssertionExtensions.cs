using System.Reflection;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Assembly type using [AssertionFrom&lt;Assembly&gt;] attributes.
/// Each assertion wraps a property from the Assembly class.
/// </summary>
#if !NETSTANDARD2_0
[AssertionFrom<Assembly>("IsCollectible", ExpectationMessage = "be collectible")]
[AssertionFrom<Assembly>("IsCollectible", CustomName = "IsNotCollectible", NegateLogic = true, ExpectationMessage = "be collectible")]
#endif

[AssertionFrom<Assembly>("IsDynamic", ExpectationMessage = "be dynamic")]
[AssertionFrom<Assembly>("IsDynamic", CustomName = "IsNotDynamic", NegateLogic = true, ExpectationMessage = "be dynamic")]

[AssertionFrom<Assembly>("IsFullyTrusted", ExpectationMessage = "be fully trusted")]
[AssertionFrom<Assembly>("IsFullyTrusted", CustomName = "IsNotFullyTrusted", NegateLogic = true, ExpectationMessage = "be fully trusted")]
public static partial class AssemblyAssertionExtensions
{
}
