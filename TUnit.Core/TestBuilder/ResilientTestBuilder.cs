using System.Reflection;
using TUnit.Core.Configuration;
using TUnit.Core.Diagnostics;
using TUnit.Core.Interfaces;

namespace TUnit.Core.TestBuilder;

/// <summary>
/// Production-ready TestBuilder with comprehensive error handling and recovery mechanisms.
/// </summary>
public class ResilientTestBuilder
{
    private readonly ITestBuilderInternal _innerBuilder;
    private readonly TestBuilderDiagnostics? _diagnostics;
    private readonly TestBuilderErrorHandler _errorHandler;
    private readonly TestBuilderConfiguration _configuration;
    
    public ResilientTestBuilder(TestBuilderConfiguration? configuration = null)
    {
        _configuration = configuration ?? TestBuilderConfiguration.Default;
        _errorHandler = new TestBuilderErrorHandler(_configuration.ErrorHandlingPolicy);
        _diagnostics = _configuration.EnableDiagnostics ? new TestBuilderDiagnostics(true) : null;
        _innerBuilder = CreateInnerBuilder(_configuration);
    }
    
    /// <summary>
    /// Builds test definitions with comprehensive error handling and recovery.
    /// </summary>
    public async Task<TestBuilderResult> BuildTestsAsync(TestMetadata metadata, CancellationToken cancellationToken = default)
    {
        var result = new TestBuilderResult();
        
        try
        {
            // Validate metadata
            ValidateMetadata(metadata);
            
            // Build tests with error recovery
            var definitions = await BuildTestsWithRecoveryAsync(metadata, cancellationToken);
            
            result.TestDefinitions.AddRange(definitions);
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add(new TestBuilderError
            {
                TestMetadata = metadata,
                Exception = ex,
                ErrorType = DetermineErrorType(ex),
                Message = $"Failed to build tests for {metadata.TestMethod.Name}: {ex.Message}"
            });
            
            // Log error
            _diagnostics?.LogError("BuildTestsAsync", ex);
            
            // Apply error handling policy
            await _errorHandler.HandleErrorAsync(ex, metadata);
        }
        
        return result;
    }
    
    private async Task<List<TestDefinition>> BuildTestsWithRecoveryAsync(
        TestMetadata metadata, 
        CancellationToken cancellationToken)
    {
        var definitions = new List<TestDefinition>();
        var retryCount = 0;
        const int maxRetries = 3;
        
        while (retryCount < maxRetries)
        {
            try
            {
                using var scope = _diagnostics?.BeginScope($"BuildTests_Attempt_{retryCount + 1}");
                
                var result = await _innerBuilder.BuildTestsAsync(metadata, cancellationToken);
                definitions.AddRange(result);
                break; // Success
            }
            catch (OperationCanceledException)
            {
                // Don't retry on cancellation
                throw;
            }
            catch (Exception ex) when (IsRetriableError(ex))
            {
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    throw new TestBuilderException(
                        $"Failed to build tests after {maxRetries} attempts", ex)
                    {
                        TestMetadata = metadata
                    };
                }
                
                // Exponential backoff
                var delay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryCount));
                await Task.Delay(delay, cancellationToken);
                
                _diagnostics?.Log($"Retrying after error: {ex.Message}", DiagnosticLevel.Warning);
            }
        }
        
        return definitions;
    }
    
    private void ValidateMetadata(TestMetadata metadata)
    {
        var errors = new List<string>();
        
        // Validate required fields
        if (metadata.TestClassType == null)
            errors.Add("TestClassType is required");
            
        if (metadata.TestMethod == null)
            errors.Add("TestMethod is required");
            
        if (string.IsNullOrWhiteSpace(metadata.TestIdTemplate))
            errors.Add("TestIdTemplate is required");
            
        if (metadata.TestClassFactory == null)
            errors.Add("TestClassFactory is required");
            
        // Validate method belongs to class
        if (metadata.TestMethod != null && metadata.TestClassType != null)
        {
            if (metadata.TestMethod.DeclaringType != metadata.TestClassType &&
                !metadata.TestClassType.IsSubclassOf(metadata.TestMethod.DeclaringType!))
            {
                errors.Add($"Test method {metadata.TestMethod.Name} does not belong to class {metadata.TestClassType.Name}");
            }
        }
        
        // Validate data sources
        ValidateDataSources(metadata.ClassDataSources, "ClassDataSources", errors);
        ValidateDataSources(metadata.MethodDataSources, "MethodDataSources", errors);
        
        foreach (var (property, dataSource) in metadata.PropertyDataSources)
        {
            if (property.DeclaringType != metadata.TestClassType &&
                !metadata.TestClassType.IsSubclassOf(property.DeclaringType!))
            {
                errors.Add($"Property {property.Name} does not belong to class {metadata.TestClassType.Name}");
            }
            
            ValidateDataSource(dataSource, $"PropertyDataSource[{property.Name}]", errors);
        }
        
        if (errors.Any())
        {
            throw new InvalidTestMetadataException(
                $"Invalid test metadata: {string.Join("; ", errors)}", 
                metadata);
        }
    }
    
    private void ValidateDataSources(IReadOnlyList<IDataSourceProvider> dataSources, string name, List<string> errors)
    {
        if (dataSources == null)
        {
            errors.Add($"{name} cannot be null");
            return;
        }
        
        for (int i = 0; i < dataSources.Count; i++)
        {
            ValidateDataSource(dataSources[i], $"{name}[{i}]", errors);
        }
    }
    
    private void ValidateDataSource(IDataSourceProvider? dataSource, string name, List<string> errors)
    {
        if (dataSource == null)
        {
            errors.Add($"{name} cannot be null");
        }
    }
    
    private bool IsRetriableError(Exception ex)
    {
        return ex switch
        {
            // Transient errors that might succeed on retry
            OutOfMemoryException => false,
            StackOverflowException => false,
            ThreadAbortException => false,
            AppDomainUnloadedException => false,
            
            // Specific retriable conditions
            TargetInvocationException tie when tie.InnerException is TimeoutException => true,
            AggregateException ae when ae.InnerExceptions.Any(e => e is TimeoutException) => true,
            
            // Default: don't retry
            _ => false
        };
    }
    
    private TestBuilderErrorType DetermineErrorType(Exception ex)
    {
        return ex switch
        {
            InvalidTestMetadataException => TestBuilderErrorType.InvalidMetadata,
            DataSourceException => TestBuilderErrorType.DataSourceFailure,
            TestInstantiationException => TestBuilderErrorType.InstantiationFailure,
            PropertyInjectionException => TestBuilderErrorType.PropertyInjectionFailure,
            OperationCanceledException => TestBuilderErrorType.Cancelled,
            OutOfMemoryException => TestBuilderErrorType.ResourceExhaustion,
            _ => TestBuilderErrorType.Unknown
        };
    }
    
    private ITestBuilderInternal CreateInnerBuilder(TestBuilderConfiguration configuration)
    {
        return configuration.BuilderMode switch
        {
            TestBuilderMode.Basic => new TestBuilderAdapter(new TestBuilder()),
            TestBuilderMode.Optimized => new TestBuilderAdapter(new TestBuilderOptimized()),
            TestBuilderMode.WithDiagnostics => new TestBuilderAdapter(
                new TestBuilderWithDiagnostics(_diagnostics)),
            _ => new TestBuilderAdapter(new TestBuilderOptimized())
        };
    }
}

/// <summary>
/// Configuration for ResilientTestBuilder.
/// </summary>
public class TestBuilderConfiguration
{
    public static TestBuilderConfiguration Default { get; } = new();
    
    /// <summary>
    /// The TestBuilder implementation mode to use.
    /// </summary>
    public TestBuilderMode BuilderMode { get; set; } = TestBuilderMode.Optimized;
    
    /// <summary>
    /// Whether to enable diagnostic logging.
    /// </summary>
    public bool EnableDiagnostics { get; set; }
    
    /// <summary>
    /// Error handling policy.
    /// </summary>
    public ErrorHandlingPolicy ErrorHandlingPolicy { get; set; } = new();
    
    /// <summary>
    /// Maximum number of concurrent test builds.
    /// </summary>
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
    
    /// <summary>
    /// Timeout for building a single test.
    /// </summary>
    public TimeSpan BuildTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Error handling policy configuration.
/// </summary>
public class ErrorHandlingPolicy
{
    /// <summary>
    /// Whether to continue building other tests when one fails.
    /// </summary>
    public bool ContinueOnError { get; set; } = true;
    
    /// <summary>
    /// Whether to collect partial results when errors occur.
    /// </summary>
    public bool CollectPartialResults { get; set; } = true;
    
    /// <summary>
    /// Maximum number of errors before stopping.
    /// </summary>
    public int MaxErrors { get; set; } = 100;
    
    /// <summary>
    /// Whether to log errors to console.
    /// </summary>
    public bool LogErrors { get; set; } = true;
}

/// <summary>
/// Result of TestBuilder operation.
/// </summary>
public class TestBuilderResult
{
    /// <summary>
    /// Whether the operation completed successfully.
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Test definitions that were successfully built.
    /// </summary>
    public List<TestDefinition> TestDefinitions { get; } = new();
    
    /// <summary>
    /// Errors that occurred during building.
    /// </summary>
    public List<TestBuilderError> Errors { get; } = new();
    
    /// <summary>
    /// Total time taken for the operation.
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }
    
    /// <summary>
    /// Gets whether there are any errors.
    /// </summary>
    public bool HasErrors => Errors.Any();
    
    /// <summary>
    /// Gets whether partial results were collected.
    /// </summary>
    public bool HasPartialResults => HasErrors && TestDefinitions.Any();
}

/// <summary>
/// Represents an error that occurred during test building.
/// </summary>
public class TestBuilderError
{
    /// <summary>
    /// The test metadata that caused the error.
    /// </summary>
    public TestMetadata? TestMetadata { get; set; }
    
    /// <summary>
    /// The exception that was thrown.
    /// </summary>
    public Exception? Exception { get; set; }
    
    /// <summary>
    /// Type of error.
    /// </summary>
    public TestBuilderErrorType ErrorType { get; set; }
    
    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// When the error occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Types of errors that can occur during test building.
/// </summary>
public enum TestBuilderErrorType
{
    Unknown,
    InvalidMetadata,
    DataSourceFailure,
    InstantiationFailure,
    PropertyInjectionFailure,
    Cancelled,
    ResourceExhaustion,
    Timeout
}

/// <summary>
/// Handles errors according to configured policy.
/// </summary>
internal class TestBuilderErrorHandler
{
    private readonly ErrorHandlingPolicy _policy;
    private int _errorCount;
    
    public TestBuilderErrorHandler(ErrorHandlingPolicy policy)
    {
        _policy = policy;
    }
    
    public async Task HandleErrorAsync(Exception exception, TestMetadata metadata)
    {
        Interlocked.Increment(ref _errorCount);
        
        if (_policy.LogErrors)
        {
            LogError(exception, metadata);
        }
        
        if (_errorCount >= _policy.MaxErrors && !_policy.ContinueOnError)
        {
            throw new TestBuilderException(
                $"Maximum error count ({_policy.MaxErrors}) exceeded", 
                exception)
            {
                TestMetadata = metadata
            };
        }
        
        await Task.CompletedTask;
    }
    
    private void LogError(Exception exception, TestMetadata metadata)
    {
        var message = $"[TestBuilder Error] {metadata.TestMethod.Name}: {exception.Message}";
        
        if (_policy.LogErrors)
        {
            Console.Error.WriteLine(message);
            
            if (exception.StackTrace != null)
            {
                Console.Error.WriteLine($"Stack trace: {exception.StackTrace}");
            }
        }
    }
}