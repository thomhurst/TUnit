using TUnit.Core;

namespace TUnit.Engine.Services.TestExecution;

/// <summary>
/// Invokes the actual test method with proper instance handling.
/// Single Responsibility: Test method invocation.
/// </summary>
internal sealed class TestMethodInvoker
{
    public ValueTask InvokeTestAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        if (test.Context.InternalDiscoveredTest?.TestExecutor is { } testExecutor)
        {
            return testExecutor.ExecuteTest(test.Context,
                () => new ValueTask(test.InvokeTestAsync(test.Context.Metadata.TestDetails.ClassInstance, cancellationToken)));
        }

        return new ValueTask(test.InvokeTestAsync(test.Context.Metadata.TestDetails.ClassInstance, cancellationToken));
    }
}