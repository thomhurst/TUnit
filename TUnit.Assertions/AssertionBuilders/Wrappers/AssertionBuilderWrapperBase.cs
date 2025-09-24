using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

/// <summary>
/// Base class for assertion builder wrappers that need to configure the last assertion
/// </summary>
public abstract class AssertionBuilderWrapperBase<TActual> : AssertionBuilder<TActual>
{
    protected readonly AssertionBuilder<TActual> InnerBuilder;

    protected AssertionBuilderWrapperBase(AssertionBuilder<TActual> innerBuilder)
        : base(default(TActual)!, innerBuilder.ActualExpression)
    {
        InnerBuilder = innerBuilder;
    }

    /// <summary>
    /// Gets the last assertion added to the builder for configuration
    /// </summary>
    protected TAssertion GetLastAssertionAs<TAssertion>() where TAssertion : BaseAssertCondition
    {
        var assertion = InnerBuilder.GetLastAssertion();
        if (assertion is null)
        {
            throw new InvalidOperationException("No assertion has been added to configure");
        }

        if (assertion is not TAssertion typedAssertion)
        {
            throw new InvalidOperationException($"Expected assertion of type {typeof(TAssertion).Name} but got {assertion.GetType().Name}");
        }

        return typedAssertion;
    }

    /// <summary>
    /// Appends a method call to the expression for display
    /// </summary>
    protected new void AppendCallerMethod(string?[] expressions, [CallerMemberName] string methodName = "")
    {
        InnerBuilder.AppendCallerMethod(expressions, methodName);
    }

    // Delegate all base class abstract methods to the inner builder
    public override TaskAwaiter GetAwaiter() => InnerBuilder.GetAwaiter();
    public override ValueTask<AssertionData> GetAssertionData() => InnerBuilder.GetAssertionData();
    public override ValueTask ProcessAssertionsAsync(AssertionData data) => InnerBuilder.ProcessAssertionsAsync(data);
    public override string? ActualExpression => InnerBuilder.ActualExpression;
    public override Stack<BaseAssertCondition> Assertions => InnerBuilder.Assertions;

    // Forward other important methods
    public override IEnumerable<BaseAssertCondition> GetAssertions() => InnerBuilder.GetAssertions();
    public override void WithAssertion(BaseAssertCondition assertion) => InnerBuilder.WithAssertion(assertion);
    public override void AppendExpression(string expression) => InnerBuilder.AppendExpression(expression);
    public override void SetBecause(string reason, string? expression) => InnerBuilder.SetBecause(reason, expression);
}