using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertionBuilders.Core;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Helpers;
using TUnit.Assertions.AssertionBuilders.Interfaces;

namespace TUnit.Assertions.AssertionBuilders;

public class AssertionEvaluator : IAssertionEvaluator
{
    private readonly List<AssertionResult> _results = [];

    async ValueTask<bool> IAssertionEvaluator.EvaluateAsync(
        ValueTask<AssertionData> assertionDataTask,
        IEnumerable<ChainedAssertion> chainedAssertions,
        IExpressionFormatter expressionFormatter)
    {
        var assertions = chainedAssertions.Select(ca => ca.Assertion).ToList();
        await EvaluateInternalAsync(assertionDataTask, assertions, expressionFormatter as ExpressionFormatter ?? new ExpressionFormatter(null));
        return true; // Return true if no exceptions were thrown
    }

    public async ValueTask<AssertionData> EvaluateAsync(
        ValueTask<AssertionData> assertionDataTask,
        IEnumerable<BaseAssertCondition> assertions,
        ExpressionFormatter expressionFormatter)
    {
        return await EvaluateInternalAsync(assertionDataTask, assertions, expressionFormatter);
    }

    public async ValueTask EvaluateAsync(IAssertionChain chain)
    {
        var assertions = chain.GetBaseAssertions().ToList();
        if (assertions.Any())
        {
            var assertionData = new AssertionData(null, null, "", DateTimeOffset.Now, DateTimeOffset.Now);
            await EvaluateInternalAsync(new ValueTask<AssertionData>(assertionData), assertions, new ExpressionFormatter(null));
        }
    }

    private async Task<AssertionData> EvaluateInternalAsync(
        ValueTask<AssertionData> assertionDataTask,
        IEnumerable<BaseAssertCondition> assertions,
        IExpressionFormatter expressionFormatter)
    {
        var assertionData = await GetAssertionDataWithTimeout(assertionDataTask, assertions);
        var currentScope = AssertionScope.GetCurrentAssertionScope();

        foreach (var assertion in assertions)
        {
            var result = await assertion.GetAssertionResult(
                assertionData.Result, 
                assertionData.Exception,
                new AssertionMetadata
                {
                    StartTime = assertionData.Start,
                    EndTime = assertionData.End
                },
                assertionData.ActualExpression);

            _results.Add(result);

            if (!result.IsPassed)
            {
                if (assertion.Subject is null)
                {
                    assertion.SetSubject(assertionData.ActualExpression);
                }

                var exception = new AssertionException(
                    $"""
                     Expected {assertion.Subject} {assertion.GetExpectationWithReason()}
                     
                     but {result.Message}
                     
                     at {expressionFormatter.GetExpression()}
                     """
                );

                if (currentScope != null)
                {
                    currentScope.AddException(exception);
                    continue;
                }

                throw exception;
            }
        }

        return assertionData;
    }

    public IEnumerable<AssertionResult> GetResults() => _results;

    private async Task<AssertionData> GetAssertionDataWithTimeout(
        ValueTask<AssertionData> assertionDataTask,
        IEnumerable<BaseAssertCondition> assertions)
    {
        var minimumWait = assertions.Select(x => x.WaitFor).Min();

        if (minimumWait is null)
        {
            return await assertionDataTask;
        }

        using var cts = new CancellationTokenSource();
        var completedTask = await Task.WhenAny(
            assertionDataTask.AsTask(), 
            GetTimeoutTask(minimumWait.Value, cts.Token)
        );

        cts.Cancel();
        return await completedTask;
    }

    private async Task<AssertionData> GetTimeoutTask(TimeSpan wait, CancellationToken token)
    {
        var start = DateTimeOffset.Now;
        await Task.Delay(wait, token);
        
        return new AssertionData(
            null, 
            new CompleteWithinException($"The assertion did not complete within {wait.PrettyPrint()}"),
            null,
            start, 
            DateTimeOffset.Now
        );
    }
}