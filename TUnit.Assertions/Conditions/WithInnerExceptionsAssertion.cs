using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts on the InnerExceptions collection of an AggregateException using an inline delegate.
/// The delegate receives a CollectionAssertion&lt;Exception&gt; for the InnerExceptions, enabling
/// full collection assertion chaining (Count, All().Satisfy, Contains, etc.).
/// </summary>
public class WithInnerExceptionsAssertion : Assertion<AggregateException>
{
    private readonly Func<CollectionAssertion<Exception>, Assertion<IEnumerable<Exception>>?> _innerExceptionsAssertion;

    internal WithInnerExceptionsAssertion(
        AssertionContext<AggregateException> context,
        Func<CollectionAssertion<Exception>, Assertion<IEnumerable<Exception>>?> innerExceptionsAssertion)
        : base(context)
    {
        _innerExceptionsAssertion = innerExceptionsAssertion;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<AggregateException> metadata)
    {
        var evaluationException = metadata.Exception;
        if (evaluationException != null)
        {
            return AssertionResult.Failed($"threw {evaluationException.GetType().FullName}");
        }

        var aggregateException = metadata.Value;

        if (aggregateException == null)
        {
            return AssertionResult.Failed("exception was null");
        }

        var collectionSource = new CollectionAssertion<Exception>(aggregateException.InnerExceptions, "InnerExceptions");
        var resultingAssertion = _innerExceptionsAssertion(collectionSource);

        if (resultingAssertion != null)
        {
            try
            {
                await resultingAssertion.AssertAsync();
            }
            catch (Exception ex)
            {
                return AssertionResult.Failed($"inner exceptions did not satisfy assertion: {ex.Message}");
            }
        }

        return AssertionResult.Passed;
    }

    protected override string GetExpectation() => "to have inner exceptions satisfying assertion";
}
