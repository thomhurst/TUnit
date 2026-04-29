using TUnit.Assertions.Core;

namespace TUnit.Assertions.Should.Core;

/// <summary>
/// Entry wrapper produced by <c>value.Should()</c>. Holds the assertion context
/// and provides the starting point for chained Should-flavored assertions.
/// </summary>
public readonly struct ShouldSource<T> : IShouldSource<T>
{
    private readonly string? _becauseMessage;

    public AssertionContext<T> Context { get; }

    public ShouldSource(AssertionContext<T> context) : this(context, becauseMessage: null)
    {
    }

    private ShouldSource(AssertionContext<T> context, string? becauseMessage)
    {
        Context = context;
        _becauseMessage = becauseMessage;
    }

    public ShouldSource<T> Because(string message)
        => new(Context, message.Trim());

    string? IShouldSource<T>.ConsumeBecauseMessage()
        => _becauseMessage;
}
