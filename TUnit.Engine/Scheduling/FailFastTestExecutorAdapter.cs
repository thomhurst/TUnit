using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core.Enums;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Test executor adapter that supports fail-fast behavior
/// </summary>
internal sealed class FailFastTestExecutorAdapter : ITestExecutor
{
    private readonly ISingleTestExecutor _innerExecutor;
    private readonly IMessageBus _messageBus;
    private readonly SessionUid _sessionUid;
    private readonly bool _isFailFastEnabled;
    private readonly CancellationTokenSource _failFastCancellationSource;
    private readonly TUnitFrameworkLogger _logger;
    
    public FailFastTestExecutorAdapter(
        ISingleTestExecutor innerExecutor,
        IMessageBus messageBus,
        SessionUid sessionUid,
        bool isFailFastEnabled,
        CancellationTokenSource failFastCancellationSource,
        TUnitFrameworkLogger logger)
    {
        _innerExecutor = innerExecutor;
        _messageBus = messageBus;
        _sessionUid = sessionUid;
        _isFailFastEnabled = isFailFastEnabled;
        _failFastCancellationSource = failFastCancellationSource;
        _logger = logger;
    }
    
    public async Task ExecuteTestAsync(ExecutableTest test, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _innerExecutor.ExecuteTestAsync(test, _messageBus, cancellationToken);
            
            // Check if we should trigger fail-fast
            if (_isFailFastEnabled && test.Result?.Status == Status.Failed)
            {
                // Log as warning since it's informational
                await _logger.LogErrorAsync($"Test {test.TestId} failed. Triggering fail-fast cancellation.");
                _failFastCancellationSource.Cancel();
            }
        }
        catch (Exception ex)
        {
            // Log the exception
            await _logger.LogErrorAsync($"Unhandled exception in test {test.TestId}: {ex}");
            
            // If fail-fast is enabled, cancel all remaining tests
            if (_isFailFastEnabled)
            {
                await _logger.LogErrorAsync("Unhandled exception occurred. Triggering fail-fast cancellation.");
                _failFastCancellationSource.Cancel();
            }
            
            // Re-throw to maintain existing behavior
            throw;
        }
    }
}