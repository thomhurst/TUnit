using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts on the InnerExceptions collection of an AggregateException using an inline delegate.
/// The delegate receives a CollectionAssertion&lt;Exception&gt; for the InnerExceptions, enabling
/// full collection assertion chaining (Count, All().Satisfy, Contains, etc.).
/// </summary>
public class WithInnerExceptionsAssertion<TException> : Assertion<TException>
    where TException : Exception
{
    private readonly Func<CollectionAssertion<Exception>, Assertion<IEnumerable<Exception>>?> _innerExceptionsAssertion;

    internal WithInnerExceptionsAssertion(
        AssertionContext<TException> context,
        Func<CollectionAssertion<Exception>, Assertion<IEnumerable<Exception>>?> innerExceptionsAssertion)
        : base(context)
    {
        _innerExceptionsAssertion = innerExceptionsAssertion;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<TException> metadata)
    {
        var exception = metadata.Value;

        if (exception is not AggregateException aggregateException)
        {
            return AssertionResult.Failed(
                exception == null
                    ? "exception was null"
                    : $"exception was {exception.GetType().Name}, not AggregateException");
        }

        var innerExceptions = aggregateException.InnerExceptions;
        var collectionSource = new CollectionAssertion<Exception>(innerExceptions, "InnerExceptions");
        var resultingAssertion = _innerExceptionsAssertion(collectionSource);

        if (resultingAssertion != null)
        {
            try
            {
                await resultingAssertion.AssertAsync();
                return AssertionResult.Passed;
            }
            catch
            {
                return AssertionResult.Failed("inner exceptions assertion failed");
            }
        }

        return AssertionResult.Passed;
    }

    protected override string GetExpectation() => "to have inner exceptions satisfying assertion";
}
