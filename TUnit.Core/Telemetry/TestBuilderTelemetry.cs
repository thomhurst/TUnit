using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace TUnit.Core.Telemetry;

/// <summary>
/// Provides telemetry and metrics collection for TestBuilder operations.
/// </summary>
public class TestBuilderTelemetry : IDisposable
{
    private static readonly Meter Meter = new("TUnit.TestBuilder", "1.0");
    private static readonly ActivitySource ActivitySource = new("TUnit.TestBuilder");
    
    // Counters
    private readonly Counter<long> _testsBuiltCounter;
    private readonly Counter<long> _testBuildErrorsCounter;
    private readonly Counter<long> _dataSourceEnumerationsCounter;
    
    // Histograms
    private readonly Histogram<double> _testBuildDurationHistogram;
    private readonly Histogram<long> _testCombinationsHistogram;
    private readonly Histogram<double> _dataSourceEnumerationDurationHistogram;
    
    // Gauges (via ObservableGauge)
    private readonly ConcurrentDictionary<string, long> _currentMetrics = new();
    
    // Tags for metrics
    private readonly KeyValuePair<string, object?>[] _commonTags;
    
    public TestBuilderTelemetry(string? instanceId = null)
    {
        _commonTags = new[]
        {
            new KeyValuePair<string, object?>("instance_id", instanceId ?? Guid.NewGuid().ToString()),
            new KeyValuePair<string, object?>("version", typeof(TestBuilderTelemetry).Assembly.GetName().Version?.ToString())
        };
        
        // Initialize counters
        _testsBuiltCounter = Meter.CreateCounter<long>(
            "tunit.testbuilder.tests_built",
            "tests",
            "Number of tests successfully built");
            
        _testBuildErrorsCounter = Meter.CreateCounter<long>(
            "tunit.testbuilder.errors",
            "errors",
            "Number of errors during test building");
            
        _dataSourceEnumerationsCounter = Meter.CreateCounter<long>(
            "tunit.testbuilder.datasource_enumerations",
            "enumerations",
            "Number of data source enumerations");
        
        // Initialize histograms
        _testBuildDurationHistogram = Meter.CreateHistogram<double>(
            "tunit.testbuilder.build_duration",
            "milliseconds",
            "Duration of test building operations");
            
        _testCombinationsHistogram = Meter.CreateHistogram<long>(
            "tunit.testbuilder.combinations",
            "combinations",
            "Number of test combinations generated");
            
        _dataSourceEnumerationDurationHistogram = Meter.CreateHistogram<double>(
            "tunit.testbuilder.datasource_duration",
            "milliseconds",
            "Duration of data source enumeration");
        
        // Observable gauges for current state
        Meter.CreateObservableGauge(
            "tunit.testbuilder.active_builds",
            () => _currentMetrics.GetValueOrDefault("active_builds", 0),
            "builds",
            "Number of active test builds");
            
        Meter.CreateObservableGauge(
            "tunit.testbuilder.cache_size",
            () => _currentMetrics.GetValueOrDefault("cache_size", 0),
            "entries",
            "Number of cached entries");
    }
    
    /// <summary>
    /// Records that tests were successfully built.
    /// </summary>
    public void RecordTestsBuilt(int count, TestMetadata metadata)
    {
        var tags = GetTags(metadata);
        _testsBuiltCounter.Add(count, tags);
    }
    
    /// <summary>
    /// Records a test build error.
    /// </summary>
    public void RecordError(Exception exception, TestMetadata? metadata = null)
    {
        var tags = GetTags(metadata, new[]
        {
            new KeyValuePair<string, object?>("error_type", exception.GetType().Name),
            new KeyValuePair<string, object?>("error_message", exception.Message)
        });
        
        _testBuildErrorsCounter.Add(1, tags);
    }
    
    /// <summary>
    /// Records the duration of a test build operation.
    /// </summary>
    public void RecordBuildDuration(TimeSpan duration, TestMetadata metadata, bool success)
    {
        var tags = GetTags(metadata, new[]
        {
            new KeyValuePair<string, object?>("success", success)
        });
        
        _testBuildDurationHistogram.Record(duration.TotalMilliseconds, tags);
    }
    
    /// <summary>
    /// Records the number of test combinations generated.
    /// </summary>
    public void RecordTestCombinations(int count, TestMetadata metadata)
    {
        _testCombinationsHistogram.Record(count, GetTags(metadata));
    }
    
    /// <summary>
    /// Records data source enumeration.
    /// </summary>
    public void RecordDataSourceEnumeration(string dataSourceType, int itemCount, TimeSpan duration)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("datasource_type", dataSourceType),
            new KeyValuePair<string, object?>("item_count_bucket", GetCountBucket(itemCount))
        }.Concat(_commonTags).ToArray();
        
        _dataSourceEnumerationsCounter.Add(1, tags);
        _dataSourceEnumerationDurationHistogram.Record(duration.TotalMilliseconds, tags);
    }
    
    /// <summary>
    /// Starts an activity for distributed tracing.
    /// </summary>
    public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        return ActivitySource.StartActivity(name, kind);
    }
    
    /// <summary>
    /// Creates a scoped operation for telemetry.
    /// </summary>
    public IDisposable BeginOperation(string operationName, TestMetadata? metadata = null)
    {
        return new TelemetryScope(this, operationName, metadata);
    }
    
    /// <summary>
    /// Updates a current metric value.
    /// </summary>
    public void UpdateMetric(string name, long value)
    {
        _currentMetrics.AddOrUpdate(name, value, (_, _) => value);
    }
    
    /// <summary>
    /// Increments a current metric value.
    /// </summary>
    public void IncrementMetric(string name, long delta = 1)
    {
        _currentMetrics.AddOrUpdate(name, delta, (_, current) => current + delta);
    }
    
    /// <summary>
    /// Decrements a current metric value.
    /// </summary>
    public void DecrementMetric(string name, long delta = 1)
    {
        _currentMetrics.AddOrUpdate(name, -delta, (_, current) => current - delta);
    }
    
    private KeyValuePair<string, object?>[] GetTags(TestMetadata? metadata, KeyValuePair<string, object?>[]? additionalTags = null)
    {
        var tags = new List<KeyValuePair<string, object?>>(_commonTags);
        
        if (metadata != null)
        {
            tags.Add(new KeyValuePair<string, object?>("test_class", metadata.TestClassType.Name));
            tags.Add(new KeyValuePair<string, object?>("test_method", metadata.TestMethod.Name));
            tags.Add(new KeyValuePair<string, object?>("is_async", metadata.IsAsync));
            tags.Add(new KeyValuePair<string, object?>("is_skipped", metadata.IsSkipped));
            tags.Add(new KeyValuePair<string, object?>("repeat_count", metadata.RepeatCount));
        }
        
        if (additionalTags != null)
        {
            tags.AddRange(additionalTags);
        }
        
        return tags.ToArray();
    }
    
    private static string GetCountBucket(int count)
    {
        return count switch
        {
            0 => "0",
            1 => "1",
            <= 10 => "2-10",
            <= 50 => "11-50",
            <= 100 => "51-100",
            <= 500 => "101-500",
            <= 1000 => "501-1000",
            _ => "1000+"
        };
    }
    
    public void Dispose()
    {
        Meter?.Dispose();
        ActivitySource?.Dispose();
    }
    
    private class TelemetryScope : IDisposable
    {
        private readonly TestBuilderTelemetry _telemetry;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        private readonly Activity? _activity;
        private readonly TestMetadata? _metadata;
        
        public TelemetryScope(TestBuilderTelemetry telemetry, string operationName, TestMetadata? metadata)
        {
            _telemetry = telemetry;
            _operationName = operationName;
            _metadata = metadata;
            _stopwatch = Stopwatch.StartNew();
            _activity = telemetry.StartActivity(operationName);
            
            if (_activity != null && metadata != null)
            {
                _activity.SetTag("test.class", metadata.TestClassType.Name);
                _activity.SetTag("test.method", metadata.TestMethod.Name);
            }
            
            _telemetry.IncrementMetric("active_builds");
        }
        
        public void Dispose()
        {
            _stopwatch.Stop();
            _activity?.Dispose();
            _telemetry.DecrementMetric("active_builds");
            
            if (_metadata != null)
            {
                _telemetry.RecordBuildDuration(_stopwatch.Elapsed, _metadata, success: true);
            }
        }
    }
}

/// <summary>
/// Extension methods for integrating telemetry with TestBuilder.
/// </summary>
public static class TestBuilderTelemetryExtensions
{
    /// <summary>
    /// Wraps a TestBuilder with telemetry collection.
    /// </summary>
    public static TestBuilderWithTelemetry WithTelemetry(this ITestBuilderInternal builder, TestBuilderTelemetry? telemetry = null)
    {
        return new TestBuilderWithTelemetry(builder, telemetry ?? new TestBuilderTelemetry());
    }
}

/// <summary>
/// TestBuilder wrapper that adds telemetry collection.
/// </summary>
public class TestBuilderWithTelemetry : ITestBuilderInternal
{
    private readonly ITestBuilderInternal _innerBuilder;
    private readonly TestBuilderTelemetry _telemetry;
    
    public TestBuilderWithTelemetry(ITestBuilderInternal innerBuilder, TestBuilderTelemetry telemetry)
    {
        _innerBuilder = innerBuilder;
        _telemetry = telemetry;
    }
    
    public async Task<IEnumerable<TestDefinition>> BuildTestsAsync(TestMetadata metadata, CancellationToken cancellationToken = default)
    {
        using var operation = _telemetry.BeginOperation("BuildTests", metadata);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var definitions = await _innerBuilder.BuildTestsAsync(metadata, cancellationToken);
            var definitionList = definitions.ToList();
            
            // Record metrics
            _telemetry.RecordTestsBuilt(definitionList.Count, metadata);
            _telemetry.RecordTestCombinations(definitionList.Count / Math.Max(1, metadata.RepeatCount), metadata);
            
            return definitionList;
        }
        catch (Exception ex)
        {
            _telemetry.RecordError(ex, metadata);
            _telemetry.RecordBuildDuration(stopwatch.Elapsed, metadata, success: false);
            throw;
        }
    }
}