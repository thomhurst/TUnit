using TUnit.Core;

namespace TUnit.Engine.Services.TestExecution;

internal static class RetryHelper
{
    public static async Task ExecuteWithRetry(TestContext testContext, Func<ValueTask> action)
    {
        var maxRetries = testContext.Metadata.TestDetails.RetryLimit;

        for (var attempt = 0; attempt < maxRetries + 1; attempt++)
        {
            testContext.CurrentRetryAttempt = attempt;

            try
            {
                await action();
                return;
            }
            catch (Exception ex)
            {
                if (attempt >= maxRetries)
                {
                    throw;
                }

                if (await ShouldRetry(testContext, ex, attempt))
                {
#if NET
                    // Stop the failed attempt's activity before retrying
                    var activity = testContext.Activity;
                    if (activity is not null)
                    {
                        activity.SetTag("test.case.result.status", "fail");
                        activity.SetTag("tunit.test.retry_attempt", attempt);
                        TUnitActivitySource.RecordException(activity, ex);
                        TUnitActivitySource.StopActivity(activity);
                        testContext.Activity = null;
                    }
#endif

                    // Apply backoff delay before retrying
                    await ApplyBackoffDelay(testContext, attempt).ConfigureAwait(false);

                    // Clear the previous result before retrying
                    testContext.Execution.Result = null;
                    testContext.TestStart = null;
                    testContext.Execution.TestEnd = null;
#pragma warning disable CS0618 // Obsolete Timing API
                    testContext.Timings.Clear();
#pragma warning restore CS0618
                    continue;
                }

                throw;
            }
        }
    }

    private static Task<bool> ShouldRetry(TestContext testContext, Exception ex, int attempt)
    {
        if (attempt >= testContext.Metadata.TestDetails.RetryLimit)
        {
            return Task.FromResult(false);
        }

        if (testContext.RetryFunc == null)
        {
            // Default behavior: retry on any exception if within retry limit
            return Task.FromResult(true);
        }

        return testContext.RetryFunc(testContext, ex, attempt + 1);
    }

    private static Task ApplyBackoffDelay(TestContext testContext, int attempt)
    {
        var backoffMs = testContext.Metadata.TestDetails.RetryBackoffMs;

        if (backoffMs <= 0)
        {
            return Task.CompletedTask;
        }

        var multiplier = testContext.Metadata.TestDetails.RetryBackoffMultiplier;
        var delayMs = (int)(backoffMs * Math.Pow(multiplier, attempt));

        if (delayMs > 0)
        {
            return Task.Delay(delayMs, testContext.CancellationToken);
        }

        return Task.CompletedTask;
    }
}
