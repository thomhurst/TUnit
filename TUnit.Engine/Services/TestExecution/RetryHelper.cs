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
                    // Clear the previous result before retrying
                    testContext.Execution.Result = null;
                    testContext.TestStart = null;
                    testContext.Execution.TestEnd = null;
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
}
