using TUnit.Core;

namespace TUnit.Engine.Services.TestExecution;

/// <summary>
/// Invokes the actual test method with proper instance handling.
/// Single Responsibility: Test method invocation.
/// </summary>
internal sealed class TestMethodInvoker
{
    public async Task InvokeTestAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        if (test.Context.InternalDiscoveredTest?.TestExecutor is { } testExecutor)
        {
            await testExecutor.ExecuteTest(test.Context,
                async () => await test.InvokeTestAsync(test.Context.TestDetails.ClassInstance, cancellationToken))
                .ConfigureAwait(false);
        }
        else
        {
            await test.InvokeTestAsync(test.Context.TestDetails.ClassInstance, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}