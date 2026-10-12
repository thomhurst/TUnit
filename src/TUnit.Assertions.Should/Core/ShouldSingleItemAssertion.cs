using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Should.Core;

/// <summary>
/// Per-call result of parameterless <c>HaveSingleItem()</c>. Wraps
/// <see cref="HasSingleItemAssertion{TCollection, TItem}"/> and forwards <c>await</c> to the
/// awaiter that type hides, so the awaited value is <typeparamref name="TItem"/>.
/// </summary>
/// <remarks>
/// Class rather than <c>readonly struct</c>, for the same reason as <see cref="ShouldAssertion{T}"/>:
/// <see cref="GetAwaiter"/> forwards to a state-machine awaiter that mutates the wrapped assertion.
/// <see cref="And"/> and <see cref="Or"/> are read through <see cref="Assertion{TValue}"/> so the
/// <c>new</c> collection continuations on <see cref="HasSingleItemAssertion{TCollection, TItem}"/>
/// are not required; generated extensions continue from <see cref="ShouldContinuation{T}"/>.
/// </remarks>
public sealed class ShouldSingleItemAssertion<TCollection, TItem> : IShouldSource<TCollection>, IAssertion
    where TCollection : IEnumerable<TItem>
{
    private readonly HasSingleItemAssertion<TCollection, TItem> _inner;

    public AssertionContext<TCollection> Context { get; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ShouldSingleItemAssertion(AssertionContext<TCollection> context, HasSingleItemAssertion<TCollection, TItem> inner)
    {
        Context = context;
        _inner = inner;
    }

    public TaskAwaiter<TItem> GetAwaiter() => _inner.GetAwaiter();

    public Task<TCollection?> AssertAsync() => _inner.AssertAsync();

    Task IAssertion.AssertAsync() => _inner.AssertAsync();

    public ShouldSingleItemAssertion<TCollection, TItem> Because(string message)
    {
        _inner.Because(message);
        return this;
    }

    public ShouldContinuation<TCollection> And => new(CollectionAssertion.And.Context);

    public ShouldContinuation<TCollection> Or => new(CollectionAssertion.Or.Context);

    string? IShouldSource<TCollection>.ConsumeBecauseMessage()
        => null;

    private Assertion<TCollection> CollectionAssertion => _inner;
}
