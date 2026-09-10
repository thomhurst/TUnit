using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

#if NET5_0_OR_GREATER
using System;

/// <summary>
/// Source-generated assertions for Memory&lt;T&gt; and ReadOnlyMemory&lt;T&gt; types using [AssertionFrom] attributes.
/// Note: Span types cannot be used due to ref struct constraints in the assertion framework.
/// Available in .NET 5.0+
/// </summary>

// Memory<T> assertions
[AssertionFrom(typeof(Memory<>), nameof(Memory<int>.IsEmpty), ExpectationMessage = "be empty")]
[AssertionFrom(typeof(Memory<>), nameof(Memory<int>.IsEmpty), CustomName = "IsNotEmpty", NegateLogic = true, ExpectationMessage = "be empty")]

// ReadOnlyMemory<T> assertions
[AssertionFrom(typeof(ReadOnlyMemory<>), nameof(ReadOnlyMemory<int>.IsEmpty), ExpectationMessage = "be empty")]
[AssertionFrom(typeof(ReadOnlyMemory<>), nameof(ReadOnlyMemory<int>.IsEmpty), CustomName = "IsNotEmpty", NegateLogic = true, ExpectationMessage = "be empty")]
public static partial class SpanAssertionExtensions
{
}
#endif
