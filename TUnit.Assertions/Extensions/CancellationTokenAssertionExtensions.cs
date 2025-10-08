using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// CancellationToken-specific assertion extension methods.
/// </summary>
public static class CancellationTokenAssertionExtensions
{
    public static CanBeCanceledAssertion CanBeCanceled(this IAssertionSource<CancellationToken> source)
    {
        source.Context.ExpressionBuilder.Append(".CanBeCanceled()");
        return new CanBeCanceledAssertion(source.Context);
    }

    public static CannotBeCanceledAssertion CannotBeCanceled(this IAssertionSource<CancellationToken> source)
    {
        source.Context.ExpressionBuilder.Append(".CannotBeCanceled()");
        return new CannotBeCanceledAssertion(source.Context);
    }

    public static IsCancellationRequestedAssertion IsCancellationRequested(this IAssertionSource<CancellationToken> source)
    {
        source.Context.ExpressionBuilder.Append(".IsCancellationRequested()");
        return new IsCancellationRequestedAssertion(source.Context);
    }

    public static IsNotCancellationRequestedAssertion IsNotCancellationRequested(this IAssertionSource<CancellationToken> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotCancellationRequested()");
        return new IsNotCancellationRequestedAssertion(source.Context);
    }

    public static IsNoneAssertion IsNone(this IAssertionSource<CancellationToken> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNone()");
        return new IsNoneAssertion(source.Context);
    }

    public static IsNotNoneAssertion IsNotNone(this IAssertionSource<CancellationToken> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotNone()");
        return new IsNotNoneAssertion(source.Context);
    }
}
