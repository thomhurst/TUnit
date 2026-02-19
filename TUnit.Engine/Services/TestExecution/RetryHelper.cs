using TUnit.Core;

namespace TUnit.Engine.Services.TestExecution;

internal static class RetryHelper
{
    public static async Task ExecuteWithRetry(TestContext testContext, Func<Task> action)
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
                    testContext.Timings.Clear();
                    continue;
                }

                throw;
            }
        }
    }

    private static async Task<bool> ShouldRetry(TestContext testContext, Exception ex, int attempt)
    {
        if (attempt >= testContext.Metadata.TestDetails.RetryLimit)
        {
            return false;
        }

        if (testContext.RetryFunc == null)
        {
            // Default behavior: retry on any exception if within retry limit
            return true;
        }

        return await testContext.RetryFunc(testContext, ex, attempt + 1).ConfigureAwait(false);
    }

    private static async Task ApplyBackoffDelay(TestContext testContext, int attempt)
    {
        var backoffMs = testContext.Metadata.TestDetails.RetryBackoffMs;

        if (backoffMs <= 0)
        {
            return;
        }

        var multiplier = testContext.Metadata.TestDetails.RetryBackoffMultiplier;
        var delayMs = (int)(backoffMs * Math.Pow(multiplier, attempt));

        if (delayMs > 0)
        {
            await Task.Delay(delayMs, testContext.CancellationToken).ConfigureAwait(false);
        }
    }
}
