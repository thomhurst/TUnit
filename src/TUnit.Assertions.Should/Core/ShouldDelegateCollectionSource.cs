using System.ComponentModel;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Should.Core;

public readonly struct ShouldDelegateCollectionSource<TItem> : IShouldSource<IEnumerable<TItem>>
{
    private readonly string? _becauseMessage;

    public AssertionContext<IEnumerable<TItem>> Context { get; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ShouldDelegateCollectionSource(AssertionContext<IEnumerable<TItem>> context)
        : this(context, becauseMessage: null)
    {
    }

    private ShouldDelegateCollectionSource(AssertionContext<IEnumerable<TItem>> context, string? becauseMessage)
    {
        Context = context;
        _becauseMessage = becauseMessage;
    }

    /// <summary>
    /// Attaches a human-readable reason to the next assertion in the chain. Returns a NEW struct —
    /// because <see cref="ShouldDelegateCollectionSource{TItem}"/> is a <c>readonly struct</c>, the
    /// result MUST be consumed inline (e.g. <c>source.Because("...").HaveAtLeast(2)</c>). Assigning
    /// it to a variable and continuing on the original copy silently drops the message.
    /// </summary>
    public ShouldDelegateCollectionSource<TItem> Because(string message)
        => new(Context, message.Trim());

    string? IShouldSource<IEnumerable<TItem>>.ConsumeBecauseMessage()
        => _becauseMessage;

    public ShouldAssertion<IEnumerable<TItem>> HaveAtLeast(
        int minCount,
        [CallerArgumentExpression(nameof(minCount))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".HaveAtLeast({expression})");
        var inner = new CollectionHasAtLeastAssertion<IEnumerable<TItem>, TItem>(Context, minCount);
        ApplyBecause(inner);
        return new ShouldAssertion<IEnumerable<TItem>>(Context, inner);
    }

    public ShouldAssertion<IEnumerable<TItem>> HaveAtMost(
        int maxCount,
        [CallerArgumentExpression(nameof(maxCount))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".HaveAtMost({expression})");
        var inner = new CollectionHasAtMostAssertion<IEnumerable<TItem>, TItem>(Context, maxCount);
        ApplyBecause(inner);
        return new ShouldAssertion<IEnumerable<TItem>>(Context, inner);
    }

    public ShouldAssertion<IEnumerable<TItem>> HaveCount(
        int expectedCount,
        [CallerArgumentExpression(nameof(expectedCount))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".HaveCount({expression})");
        var inner = new CollectionCountAssertion<IEnumerable<TItem>, TItem>(Context, expectedCount);
        ApplyBecause(inner);
        return new ShouldAssertion<IEnumerable<TItem>>(Context, inner);
    }

    public ShouldAssertion<TException> Throw<TException>() where TException : Exception
    {
        Context.ExpressionBuilder.Append($".Throw<{DelegateExceptionTypeFormatter.FormatTypeName(typeof(TException))}>()");
        var mapped = Context.MapException<TException>();
        var inner = new ThrowsAssertion<TException>(mapped);
        ApplyBecause(inner);
        return new ShouldAssertion<TException>(mapped, inner);
    }

    public ShouldAssertion<TException> ThrowExactly<TException>() where TException : Exception
    {
        Context.ExpressionBuilder.Append($".ThrowExactly<{DelegateExceptionTypeFormatter.FormatTypeName(typeof(TException))}>()");
        var mapped = Context.MapException<TException>();
        var inner = new ThrowsExactlyAssertion<TException>(mapped);
        ApplyBecause(inner);
        return new ShouldAssertion<TException>(mapped, inner);
    }

    private void ApplyBecause<TAssertionValue>(Assertion<TAssertionValue> assertion)
    {
        if (_becauseMessage is not null)
        {
            assertion.Because(_becauseMessage);
        }
    }

    internal static AssertionContext<IEnumerable<TItem>> CreateContext(Func<IEnumerable<TItem>?> func, string? expression)
    {
        var expressionBuilder = ShouldExpressionBuilder.Build(expression);
        var evaluationContext = new EvaluationContext<IEnumerable<TItem>>(() =>
        {
            try
            {
                return Task.FromResult<(IEnumerable<TItem>?, Exception?)>((func(), null));
            }
            catch (Exception ex)
            {
                return Task.FromResult<(IEnumerable<TItem>?, Exception?)>((default, ex));
            }
        });
        return new AssertionContext<IEnumerable<TItem>>(evaluationContext, expressionBuilder);
    }

    internal static AssertionContext<IEnumerable<TItem>> CreateContext(Func<Task<IEnumerable<TItem>?>> func, string? expression)
    {
        var expressionBuilder = ShouldExpressionBuilder.Build(expression);
        var evaluationContext = new EvaluationContext<IEnumerable<TItem>>(async () =>
        {
            try
            {
                return (await func().ConfigureAwait(false), null);
            }
            catch (Exception ex)
            {
                return (default, ex);
            }
        });
        return new AssertionContext<IEnumerable<TItem>>(evaluationContext, expressionBuilder);
    }
}
