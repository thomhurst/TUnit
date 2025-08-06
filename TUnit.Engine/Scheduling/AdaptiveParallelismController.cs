using TUnit.Core.Logging;
using TUnit.Engine.Logging;
using TUnit.Engine.Services;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Controller that runs in the background and adjusts parallelism based on system metrics
/// </summary>
internal sealed class AdaptiveParallelismController : IDisposable
{
    private readonly AdaptiveSemaphore _semaphore;
    private readonly SystemMetricsCollector _metricsCollector;
    private readonly ParallelismAdjustmentStrategy _adjustmentStrategy;
    private readonly TUnitFrameworkLogger _logger;
    private readonly bool _enableMetricsLogging;
    private readonly CancellationTokenSource _cancellationSource;
    private readonly Task _adjustmentTask;
    private readonly Task? _metricsLoggingTask;
    private int _currentParallelism;
    private bool _disposed;

    public AdaptiveParallelismController(
        AdaptiveSemaphore semaphore,
        TUnitFrameworkLogger logger,
        int minParallelism,
        int maxParallelism,
        int initialParallelism,
        bool enableMetricsLogging)
    {
        _semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _enableMetricsLogging = enableMetricsLogging;
        _currentParallelism = initialParallelism;
        
        _metricsCollector = new SystemMetricsCollector();
        _adjustmentStrategy = new ParallelismAdjustmentStrategy(minParallelism, maxParallelism);
        _cancellationSource = new CancellationTokenSource();
        
        // Start background tasks
        _adjustmentTask = RunAdjustmentLoopAsync(_cancellationSource.Token);
        
        if (_enableMetricsLogging)
        {
            _metricsLoggingTask = RunMetricsLoggingLoopAsync(_cancellationSource.Token);
        }
    }

    /// <summary>
    /// Gets the current parallelism level
    /// </summary>
    public int CurrentParallelism => _currentParallelism;

    /// <summary>
    /// Records a test completion for metrics
    /// </summary>
    public void RecordTestCompletion(TimeSpan executionTime)
    {
        _adjustmentStrategy.RecordTestCompletion(executionTime);
    }

    private async Task RunAdjustmentLoopAsync(CancellationToken cancellationToken)
    {
#if NET6_0_OR_GREATER
        // Use PeriodicTimer for cleaner async timing (500ms intervals)
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(cancellationToken);
                await AdjustParallelismAsync();
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                // Log error but don't crash the adjustment loop
                await _logger.LogErrorAsync($"Error in adaptive parallelism adjustment: {ex.Message}");
            }
        }
#else
        // Fallback for netstandard2.0
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(500, cancellationToken);
                await AdjustParallelismAsync();
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                // Log error but don't crash the adjustment loop
                await _logger.LogErrorAsync($"Error in adaptive parallelism adjustment: {ex.Message}");
            }
        }
#endif
    }

    private async Task RunMetricsLoggingLoopAsync(CancellationToken cancellationToken)
    {
        // Initial delay to let tests start
        await Task.Delay(1000, cancellationToken);

#if NET6_0_OR_GREATER
        // Use PeriodicTimer for metrics logging (3 second intervals)
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(cancellationToken);
                
                // Get current metrics
                var metrics = _metricsCollector.GetMetrics();
                await LogMetrics(metrics);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                // Log error but continue
                await _logger.LogErrorAsync($"Error logging adaptive metrics: {ex.Message}");
            }
        }
#else
        // Fallback for netstandard2.0
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(3000, cancellationToken);
                
                // Get current metrics
                var metrics = _metricsCollector.GetMetrics();
                await LogMetrics(metrics);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                // Log error but continue
                await _logger.LogErrorAsync($"Error logging adaptive metrics: {ex.Message}");
            }
        }
#endif
    }

    private async Task AdjustParallelismAsync()
    {
        // Collect metrics
        var metrics = _metricsCollector.GetMetrics();
        
        // Calculate adjustment
        var recommendation = _adjustmentStrategy.CalculateAdjustment(metrics, _currentParallelism);
        
        // Apply adjustment if needed
        if (recommendation.NewParallelism != _currentParallelism)
        {
            _semaphore.AdjustMaxCount(recommendation.NewParallelism);
            var oldParallelism = _currentParallelism;
            _currentParallelism = recommendation.NewParallelism;
            
            if (_enableMetricsLogging)
            {
                await LogAdjustment(oldParallelism, recommendation, metrics);
            }
        }
    }

    private async Task LogAdjustment(int oldParallelism, AdjustmentRecommendation recommendation, SystemMetrics metrics)
    {
        var direction = recommendation.Direction == AdjustmentDirection.Increase ? "↑" : "↓";
        await _logger.LogDebugAsync(
            $"[Adaptive] Parallelism adjusted: {oldParallelism} {direction} {recommendation.NewParallelism} | " +
            $"Reason: {recommendation.Reason} | " +
            $"CPU: {metrics.SystemCpuUsagePercent:F1}% | " +
            $"Threads: {metrics.AvailableWorkerThreads}/{metrics.MaxWorkerThreads} | " +
            $"Memory: {metrics.TotalMemoryBytes / 1_000_000}MB");
    }

    private async Task LogMetrics(SystemMetrics metrics)
    {
        var semaphoreAvailable = _semaphore.CurrentCount;
        var activeTests = _currentParallelism - semaphoreAvailable;
        
        await _logger.LogDebugAsync(
            $"[Adaptive] Metrics | " +
            $"Parallelism: {_currentParallelism} (Active: {activeTests}, Available: {semaphoreAvailable}) | " +
            $"CPU: {metrics.SystemCpuUsagePercent:F1}% | " +
            $"Threads: {metrics.AvailableWorkerThreads}/{metrics.MaxWorkerThreads} | " +
            $"Pending: {metrics.PendingWorkItems} | " +
            $"Memory: {metrics.TotalMemoryBytes / 1_000_000}MB");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        
        // Cancel background tasks
        _cancellationSource.Cancel();
        
        // Wait for tasks to complete (with timeout)
        try
        {
            var tasksToWait = new[] { _adjustmentTask, _metricsLoggingTask }
                .Where(t => t != null)
                .Cast<Task>()
                .ToArray();
            
            if (tasksToWait.Length > 0)
            {
                Task.WaitAll(tasksToWait, TimeSpan.FromSeconds(5));
            }
        }
        catch (AggregateException)
        {
            // Tasks may have been cancelled, which is expected
        }
        
        _cancellationSource.Dispose();
        _metricsCollector?.Dispose();
    }
}