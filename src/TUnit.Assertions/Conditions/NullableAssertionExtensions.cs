using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Nullable&lt;T&gt; type using [AssertionFrom&lt;Nullable&lt;T&gt;&gt;] attributes.
/// These wrap nullable value checks as extension methods.
/// </summary>
[AssertionFrom(typeof(int?), nameof(Nullable<int>.HasValue), ExpectationMessage = "have a value")]
[AssertionFrom(typeof(int?), nameof(Nullable<int>.HasValue), CustomName = "DoesNotHaveValue", NegateLogic = true, ExpectationMessage = "have a value")]
public static partial class NullableAssertionExtensions
{
}
