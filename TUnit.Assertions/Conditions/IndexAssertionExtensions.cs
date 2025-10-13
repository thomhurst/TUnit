#if NET6_0_OR_GREATER
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Index type using [AssertionFrom&lt;Index&gt;] attributes.
/// Each assertion wraps a property from the Index struct.
/// </summary>
[AssertionFrom<Index>(nameof(Index.IsFromEnd), ExpectationMessage = "be from the end")]
[AssertionFrom<Index>(nameof(Index.IsFromEnd), CustomName = "IsNotFromEnd", NegateLogic = true, ExpectationMessage = "be from the end")]
public static partial class IndexAssertionExtensions
{
}
#endif
