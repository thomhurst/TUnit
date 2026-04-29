using System.ComponentModel;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Should.Core;

/// <summary>
/// Per-call result of a generated Should extension method. Wraps the underlying
/// <see cref="Assertion{T}"/> produced by the original TUnit.Assertions extension
/// and forwards <c>await</c> to it. Exposes <c>.And</c>/<c>.Or</c> properties that
/// return Should-flavored continuations to keep the chain in Should naming.
/// </summary>
/// <remarks>
/// Class rather than <c>readonly struct</c>: <see cref="GetAwaiter"/> forwards to the wrapped
/// <see cref="Assertion{T}"/>'s state-machine awaiter, which mutates instance state during
/// evaluation. A struct ShouldAssertion would either copy that mutation away from the captured
/// receiver or have to box-and-shelve through <see cref="IShouldSource{T}"/>, neither of which
/// is desirable. The single per-call allocation is the conscious cost; the entry types
/// (<c>ShouldSource</c>, <c>ShouldContinuation</c>) stay structs because they're pure routers.
/// </remarks>
public sealed class ShouldAssertion<T> : IShouldSource<T>
{
    private readonly Assertion<T> _inner;

    public AssertionContext<T> Context { get; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ShouldAssertion(AssertionContext<T> context, Assertion<T> inner)
    {
        Context = context;
        _inner = inner;
    }

    public TaskAwaiter<T?> GetAwaiter() => _inner.GetAwaiter();

    public ShouldAssertion<T> Because(string message)
    {
        _inner.Because(message);
        return this;
    }

    public ShouldContinuation<T> And => new(_inner.And.Context);

    public ShouldContinuation<T> Or => new(_inner.Or.Context);

    string? IShouldSource<T>.ConsumeBecauseMessage()
        => null;
}
