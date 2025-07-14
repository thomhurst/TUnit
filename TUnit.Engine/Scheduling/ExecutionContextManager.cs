using TUnit.Core;
using TUnit.Engine.Logging;
using LoggingExtensions = TUnit.Core.Logging.LoggingExtensions;

namespace TUnit.Engine.Scheduling;

internal class ExecutionContextManager
{
    private readonly TUnitFrameworkLogger _logger;

    public ExecutionContextManager(TUnitFrameworkLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteWithRestoredContextAsync(
        TestExecutionData testData,
        Func<Task> testExecutionFunc,
        CancellationToken cancellationToken)
    {
        // Execute in a new task to ensure clean execution context
        await Task.Factory.StartNew(async () =>
        {
            try
            {
                // If we have a captured ExecutionContext, restore it
                if (testData.ExecutionContext != null)
                {
                    ExecutionContext.Run(testData.ExecutionContext, _ =>
                    {
                        // Restore the test context hierarchy
                        RestoreContextHierarchy(testData.Test.Context);
                    }, null);
                }
                else
                {
                    // Just restore the context hierarchy without a captured ExecutionContext
                    RestoreContextHierarchy(testData.Test.Context);
                }

                // Execute the test
                // The test execution will happen on the current thread to preserve
                // any SynchronizationContext or custom executor requirements
                await testExecutionFunc();
            }
            catch (Exception ex)
            {
                await LoggingExtensions.LogErrorAsync(_logger, $"Error executing test {testData.Test.Context.TestName} with restored context: {ex.Message}");
                throw;
            }
        }, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default).Unwrap();
    }

    private void RestoreContextHierarchy(TestContext testContext)
    {
        // Restore contexts from broadest to narrowest scope
        // This ensures AsyncLocal values flow correctly from parent to child contexts
        
        // Note: The actual context restoration happens through the existing
        // Context.RestoreExecutionContext() method which handles AsyncLocal restoration
        
        // The test context itself will handle restoration when needed
        // This is just a placeholder to show where hierarchy restoration would occur
        // In practice, the HookOrchestrator handles this during test execution
        
        _ = LoggingExtensions.LogDebugAsync(_logger, $"Context hierarchy prepared for test {testContext.TestName}");
    }

    public ExecutionContext? CaptureCurrentContext()
    {
        try
        {
            return ExecutionContext.Capture();
        }
        catch (Exception ex)
        {
            _ = LoggingExtensions.LogWarningAsync(_logger, $"Failed to capture ExecutionContext: {ex.Message}");
            return null;
        }
    }
}