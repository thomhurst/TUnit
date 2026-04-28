using TUnit.Assertions.Core;

namespace TUnit.Assertions.Should.Core;

/// <summary>
/// Entry wrapper produced by <c>value.Should()</c>. Holds the assertion context
/// and provides the starting point for chained Should-flavored assertions.
/// </summary>
public readonly struct ShouldSource<T> : IShouldSource<T>
{
    public AssertionContext<T> Context { get; }

    public ShouldSource(AssertionContext<T> context) => Context = context;
}
