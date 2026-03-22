using TUnit.Core;

namespace TUnit.Engine.Services.TestExecution;

/// <summary>
/// Invokes the actual test method with proper instance handling.
/// Single Responsibility: Test method invocation.
/// </summary>
internal sealed class TestMethodInvoker
{
    public Task InvokeTestAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        if (test.Context.InternalDiscoveredTest?.TestExecutor is { } testExecutor)
        {
            return testExecutor.ExecuteTest(test.Context,
                () => new ValueTask(test.InvokeTestAsync(test.Context.Metadata.TestDetails.ClassInstance, cancellationToken)))
                .AsTask();
        }

        return test.InvokeTestAsync(test.Context.Metadata.TestDetails.ClassInstance, cancellationToken);
    }
}