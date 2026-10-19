#if NET5_0_OR_GREATER
using System;

namespace TUnit.Assertions.Conditions
{
    /// <summary>
    /// No longer holds any assertions. It only ever carried [AssertionFrom] declarations for Memory&lt;T&gt; and
    /// ReadOnlyMemory&lt;T&gt; that generated nothing; IsEmpty() and IsNotEmpty() for those types are on the source
    /// returned by Assert.That. Kept so that removing it is not a breaking change.
    /// </summary>
    [Obsolete("Has no members. Memory<T> and ReadOnlyMemory<T> assertions such as IsEmpty() are on MemoryAssertionBase.")]
    public static class SpanAssertionExtensions
    {
    }
}

namespace TUnit.Assertions.Extensions
{
    /// <summary>
    /// No longer holds any assertions; see <see cref="TUnit.Assertions.Conditions.SpanAssertionExtensions"/>.
    /// Kept so that removing it is not a breaking change.
    /// </summary>
    [Obsolete("Has no members. Memory<T> and ReadOnlyMemory<T> assertions such as IsEmpty() are on MemoryAssertionBase.")]
    public static class SpanAssertionExtensions
    {
    }
}
#endif
