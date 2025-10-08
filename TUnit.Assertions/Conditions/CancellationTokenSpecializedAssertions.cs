using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

public class CanBeCanceledAssertion : Assertion<CancellationToken>
{
    public CanBeCanceledAssertion(AssertionContext<CancellationToken> context) : base(context) { }
    protected override string GetExpectation() => "to be cancellable";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<CancellationToken> metadata)
    {
        if (metadata.Value.CanBeCanceled)
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be cancellable, but it was not"));
    }
}

public class CannotBeCanceledAssertion : Assertion<CancellationToken>
{
    public CannotBeCanceledAssertion(AssertionContext<CancellationToken> context) : base(context) { }
    protected override string GetExpectation() => "to not be cancellable";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<CancellationToken> metadata)
    {
        if (!metadata.Value.CanBeCanceled)
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not be cancellable, but it was"));
    }
}

public class IsCancellationRequestedAssertion : Assertion<CancellationToken>
{
    public IsCancellationRequestedAssertion(AssertionContext<CancellationToken> context) : base(context) { }
    protected override string GetExpectation() => "to have cancellation requested";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<CancellationToken> metadata)
    {
        if (metadata.Value.IsCancellationRequested)
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to have cancellation requested, but it did not"));
    }
}

public class IsNotCancellationRequestedAssertion : Assertion<CancellationToken>
{
    public IsNotCancellationRequestedAssertion(AssertionContext<CancellationToken> context) : base(context) { }
    protected override string GetExpectation() => "to not have cancellation requested";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<CancellationToken> metadata)
    {
        if (!metadata.Value.IsCancellationRequested)
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not have cancellation requested, but it did"));
    }
}

public class IsNoneAssertion : Assertion<CancellationToken>
{
    public IsNoneAssertion(AssertionContext<CancellationToken> context) : base(context) { }
    protected override string GetExpectation() => "to be CancellationToken.None";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<CancellationToken> metadata)
    {
        if (metadata.Value.Equals(CancellationToken.None))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be CancellationToken.None, but it was not"));
    }
}

public class IsNotNoneAssertion : Assertion<CancellationToken>
{
    public IsNotNoneAssertion(AssertionContext<CancellationToken> context) : base(context) { }
    protected override string GetExpectation() => "to not be CancellationToken.None";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<CancellationToken> metadata)
    {
        if (!metadata.Value.Equals(CancellationToken.None))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not be CancellationToken.None, but it was"));
    }
}
