using TUnit.Core;

namespace TUnit.Engine.Services.TestExecution;

internal static class RetryHelper
{
    public static async Task ExecuteWithRetry(TestContext testContext, Func<Task> action)
    {
        var maxRetries = testContext.TestDetails.RetryLimit;

        for (var attempt = 0; attempt < maxRetries + 1; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception ex)
            {
                if (attempt >= maxRetries + 1)
                {
                    throw;
                }

                if (await ShouldRetry(testContext, ex, attempt))
                {
                    continue;
                }

                throw;
            }
        }
    }

    private static async Task<bool> ShouldRetry(TestContext testContext, Exception ex, int attempt)
    {
        return attempt < testContext.TestDetails.RetryLimit
            && testContext.RetryFunc != null
            && await testContext.RetryFunc(testContext, ex, attempt + 1).ConfigureAwait(false);
    }
}
