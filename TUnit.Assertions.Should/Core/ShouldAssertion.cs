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
}
