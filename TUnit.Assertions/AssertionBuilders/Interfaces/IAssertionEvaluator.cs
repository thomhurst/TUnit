using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertionBuilders.Core;

namespace TUnit.Assertions.AssertionBuilders.Interfaces;

/// <summary>
/// Internal interface for evaluating assertions
/// </summary>
internal interface IAssertionEvaluator
{
    /// <summary>
    /// Evaluates the assertion chain asynchronously
    /// </summary>
    ValueTask<bool> EvaluateAsync(
        ValueTask<AssertionData> assertionDataTask,
        IEnumerable<ChainedAssertion> assertions,
        IExpressionFormatter expressionFormatter);

    /// <summary>
    /// Gets the results of the evaluation
    /// </summary>
    IEnumerable<AssertionResult> GetResults();
}