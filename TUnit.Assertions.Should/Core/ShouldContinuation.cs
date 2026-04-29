using TUnit.Assertions.Core;

namespace TUnit.Assertions.Should.Core;

/// <summary>
/// Should-flavored chain link returned by <see cref="ShouldAssertion{T}.And"/> /
/// <see cref="ShouldAssertion{T}.Or"/>. Generated extensions resolve only against
/// <see cref="IShouldSource{T}"/>, so the chain stays in Should naming throughout.
/// </summary>
public readonly struct ShouldContinuation<T> : IShouldSource<T>
{
    private readonly string? _becauseMessage;

    public AssertionContext<T> Context { get; }

    public ShouldContinuation(AssertionContext<T> context) : this(context, becauseMessage: null)
    {
    }

    private ShouldContinuation(AssertionContext<T> context, string? becauseMessage)
    {
        Context = context;
        _becauseMessage = becauseMessage;
    }

    public ShouldContinuation<T> Because(string message)
        => new(Context, message.Trim());

    string? IShouldSource<T>.ConsumeBecauseMessage()
        => _becauseMessage;
}
