using TUnit.Core;

namespace TUnit.Engine;

/// <summary>
/// Executable test that represents a test that failed during data source expansion
/// </summary>
internal sealed class FailedExecutableTest : AbstractExecutableTest
{
    private readonly Exception _exception;

    public FailedExecutableTest(Exception exception)
    {
        _exception = exception;
    }

    public override Task<object> CreateInstanceAsync()
    {
        throw new InvalidOperationException(
            $"Failed to expand data source for test '{Context.GetDisplayName()}': {_exception.Message}",
            _exception);
    }

    public override Task InvokeTestAsync(object instance, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(
            $"Failed to expand data source for test '{Context.GetDisplayName()}': {_exception.Message}",
            _exception);
    }
}
