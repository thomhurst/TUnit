using System.Collections.Concurrent;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.Services;
#if !NETSTANDARD2_0
using System.Threading.Channels;
#endif

namespace TUnit.Engine.Execution;

/// <summary>
/// Executes tests as they are discovered via streaming
/// </summary>
internal sealed class StreamingTestExecutor
{
    private readonly UnifiedTestExecutor _executor;
    private readonly TestDependencyResolver _dependencyResolver;
#if !NETSTANDARD2_0
    private readonly Channel<ExecutableTest> _readyTests;
#else
    private readonly ConcurrentQueue<ExecutableTest> _readyTests;
    private readonly SemaphoreSlim _readyTestsSemaphore;
#endif
    
    public StreamingTestExecutor(
        UnifiedTestExecutor executor,
        TestDependencyResolver dependencyResolver)
    {
        _executor = executor;
        _dependencyResolver = dependencyResolver;
#if !NETSTANDARD2_0
        _readyTests = Channel.CreateUnbounded<ExecutableTest>();
#else
        _readyTests = new ConcurrentQueue<ExecutableTest>();
        _readyTestsSemaphore = new SemaphoreSlim(0);
#endif
    }
    
    public async Task ExecuteStreamingTests(
        IAsyncEnumerable<ExecutableTest> testStream,
        ITestExecutionFilter? filter,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        // Start execution pipeline
        var executionTask = ProcessReadyTests(filter, messageBus, cancellationToken);
        
        // Feed tests into pipeline as discovered
        await foreach (var test in testStream.WithCancellation(cancellationToken))
        {
            if (IsTestReady(test))
            {
#if !NETSTANDARD2_0
                await _readyTests.Writer.WriteAsync(test, cancellationToken);
#else
                _readyTests.Enqueue(test);
                _readyTestsSemaphore.Release();
#endif
            }
        }
        
#if !NETSTANDARD2_0
        _readyTests.Writer.Complete();
#else
        // Signal completion by releasing semaphore with int.MaxValue
        _readyTestsSemaphore.Release(int.MaxValue);
#endif
        await executionTask;
    }
    
    private bool IsTestReady(ExecutableTest test)
    {
        // Test is ready if it has no dependencies or all are resolved
        return test.Metadata.Dependencies.Length == 0 || 
               _dependencyResolver.TryResolveDependencies(test);
    }
    
    private async Task ProcessReadyTests(
        ITestExecutionFilter? filter,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
#if !NETSTANDARD2_0
        await foreach (var test in _readyTests.Reader.ReadAllAsync(cancellationToken))
        {
            // Execute using existing executor
            await _executor.ExecuteTests([test], filter, messageBus, cancellationToken);
        }
#else
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _readyTestsSemaphore.WaitAsync(cancellationToken);
                
                if (_readyTests.TryDequeue(out var test))
                {
                    await _executor.ExecuteTests([test], filter, messageBus, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
#endif
    }
}