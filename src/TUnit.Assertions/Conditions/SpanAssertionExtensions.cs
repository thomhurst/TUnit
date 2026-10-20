#if NET5_0_OR_GREATER
namespace TUnit.Assertions.Conditions
{
    /// <summary>
    /// Holds no assertions. Its [AssertionFrom] declarations for Memory&lt;T&gt; and ReadOnlyMemory&lt;T&gt; never generated
    /// anything (#7132), and IsEmpty() and IsNotEmpty() for those types are already on the source returned by
    /// Assert.That (MemoryAssertionBase), so they were removed rather than turned into duplicate public assertions.
    /// </summary>
    public static partial class SpanAssertionExtensions
    {
    }
}

namespace TUnit.Assertions.Extensions
{
    /// <summary>
    /// The (empty) extension class that the [AssertionFrom] declarations on
    /// <see cref="TUnit.Assertions.Conditions.SpanAssertionExtensions"/> used to generate. Kept so that removing
    /// those declarations is not a breaking change.
    /// </summary>
    public static partial class SpanAssertionExtensions
    {
    }
}
#endif
