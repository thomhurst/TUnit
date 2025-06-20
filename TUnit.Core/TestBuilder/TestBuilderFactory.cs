using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Configuration;
using TUnit.Core.Diagnostics;
using TUnit.Core.Telemetry;

namespace TUnit.Core.TestBuilder;

/// <summary>
/// Factory for creating TestBuilder instances with appropriate configuration.
/// </summary>
public static class TestBuilderFactory
{
    private static readonly object Lock = new();
    private static ITestBuilderFactory? _defaultFactory;
    
    /// <summary>
    /// Gets or sets the default factory implementation.
    /// </summary>
    public static ITestBuilderFactory Default
    {
        get
        {
            if (_defaultFactory == null)
            {
                lock (Lock)
                {
                    _defaultFactory ??= new DefaultTestBuilderFactory();
                }
            }
            return _defaultFactory;
        }
        set
        {
            lock (Lock)
            {
                _defaultFactory = value;
            }
        }
    }
    
    /// <summary>
    /// Creates a TestBuilder using the default factory.
    /// </summary>
    public static ITestBuilderInternal Create()
    {
        return Default.Create();
    }
    
    /// <summary>
    /// Creates a TestBuilder with specific configuration.
    /// </summary>
    public static ITestBuilderInternal Create(TestBuilderConfiguration configuration)
    {
        return Default.Create(configuration);
    }
    
    /// <summary>
    /// Creates a TestBuilder using dependency injection.
    /// </summary>
    public static ITestBuilderInternal Create(IServiceProvider serviceProvider)
    {
        return Default.Create(serviceProvider);
    }
}

/// <summary>
/// Interface for TestBuilder factory implementations.
/// </summary>
public interface ITestBuilderFactory
{
    /// <summary>
    /// Creates a TestBuilder with default configuration.
    /// </summary>
    ITestBuilderInternal Create();
    
    /// <summary>
    /// Creates a TestBuilder with specific configuration.
    /// </summary>
    ITestBuilderInternal Create(TestBuilderConfiguration configuration);
    
    /// <summary>
    /// Creates a TestBuilder using dependency injection.
    /// </summary>
    ITestBuilderInternal Create(IServiceProvider serviceProvider);
}

/// <summary>
/// Default implementation of TestBuilder factory.
/// </summary>
public class DefaultTestBuilderFactory : ITestBuilderFactory
{
    public ITestBuilderInternal Create()
    {
        var configuration = GetConfigurationFromEnvironment();
        return Create(configuration);
    }
    
    public ITestBuilderInternal Create(TestBuilderConfiguration configuration)
    {
        // Create base builder
        ITestBuilderInternal builder = CreateBaseBuilder(configuration);
        
        // Apply decorators based on configuration
        builder = ApplyDecorators(builder, configuration);
        
        return builder;
    }
    
    public ITestBuilderInternal Create(IServiceProvider serviceProvider)
    {
        // Try to get configuration from DI
        var configuration = serviceProvider.GetService<TestBuilderConfiguration>() 
                          ?? GetConfigurationFromEnvironment();
        
        // Create base builder
        ITestBuilderInternal builder = CreateBaseBuilder(configuration);
        
        // Apply decorators with DI support
        builder = ApplyDecoratorsWithDI(builder, configuration, serviceProvider);
        
        return builder;
    }
    
    private static TestBuilderConfiguration GetConfigurationFromEnvironment()
    {
        return new TestBuilderConfiguration
        {
            BuilderMode = TUnitConfiguration.TestBuilderMode,
            EnableDiagnostics = TUnitConfiguration.EnableTestBuilderDiagnostics,
            ErrorHandlingPolicy = new ErrorHandlingPolicy
            {
                ContinueOnError = GetEnvironmentBool("TUNIT_CONTINUE_ON_ERROR", true),
                CollectPartialResults = GetEnvironmentBool("TUNIT_COLLECT_PARTIAL_RESULTS", true),
                MaxErrors = GetEnvironmentInt("TUNIT_MAX_ERRORS", 100),
                LogErrors = GetEnvironmentBool("TUNIT_LOG_ERRORS", true)
            },
            MaxConcurrency = GetEnvironmentInt("TUNIT_MAX_CONCURRENCY", Environment.ProcessorCount),
            BuildTimeout = TimeSpan.FromMilliseconds(GetEnvironmentInt("TUNIT_BUILD_TIMEOUT", 30000))
        };
    }
    
    private static ITestBuilderInternal CreateBaseBuilder(TestBuilderConfiguration configuration)
    {
        return configuration.BuilderMode switch
        {
            TestBuilderMode.Basic => new TestBuilderAdapter(new TestBuilder()),
            TestBuilderMode.Optimized => new TestBuilderAdapter(new TestBuilderOptimized()),
            TestBuilderMode.WithDiagnostics => new TestBuilderAdapter(
                new TestBuilderWithDiagnostics(
                    new TestBuilderDiagnostics(configuration.EnableDiagnostics))),
            _ => new TestBuilderAdapter(new TestBuilderOptimized())
        };
    }
    
    private static ITestBuilderInternal ApplyDecorators(
        ITestBuilderInternal builder, 
        TestBuilderConfiguration configuration)
    {
        // Apply error handling
        if (configuration.ErrorHandlingPolicy.ContinueOnError)
        {
            builder = new ErrorHandlingTestBuilderDecorator(builder, configuration.ErrorHandlingPolicy);
        }
        
        // Apply telemetry if enabled
        if (GetEnvironmentBool("TUNIT_ENABLE_TELEMETRY", false))
        {
            var telemetry = new TestBuilderTelemetry();
            builder = builder.WithTelemetry(telemetry);
        }
        
        // Apply timeout enforcement
        if (configuration.BuildTimeout > TimeSpan.Zero)
        {
            builder = new TimeoutTestBuilderDecorator(builder, configuration.BuildTimeout);
        }
        
        // Apply caching if enabled
        if (GetEnvironmentBool("TUNIT_ENABLE_CACHING", true))
        {
            builder = new CachingTestBuilderDecorator(builder);
        }
        
        return builder;
    }
    
    private static ITestBuilderInternal ApplyDecoratorsWithDI(
        ITestBuilderInternal builder,
        TestBuilderConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        // Apply basic decorators
        builder = ApplyDecorators(builder, configuration);
        
        // Apply DI-specific decorators
        var telemetry = serviceProvider.GetService<TestBuilderTelemetry>();
        if (telemetry != null)
        {
            builder = builder.WithTelemetry(telemetry);
        }
        
        // Apply custom decorators from DI
        var customDecorators = serviceProvider.GetServices<ITestBuilderDecorator>();
        foreach (var decorator in customDecorators)
        {
            builder = decorator.Decorate(builder);
        }
        
        return builder;
    }
    
    private static bool GetEnvironmentBool(string name, bool defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }
    
    private static int GetEnvironmentInt(string name, int defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }
}

/// <summary>
/// Interface for TestBuilder decorators.
/// </summary>
public interface ITestBuilderDecorator
{
    /// <summary>
    /// Decorates a TestBuilder instance.
    /// </summary>
    ITestBuilderInternal Decorate(ITestBuilderInternal builder);
}

/// <summary>
/// Decorator that adds error handling to TestBuilder.
/// </summary>
internal class ErrorHandlingTestBuilderDecorator : ITestBuilderInternal
{
    private readonly ITestBuilderInternal _inner;
    private readonly ErrorHandlingPolicy _policy;
    
    public ErrorHandlingTestBuilderDecorator(ITestBuilderInternal inner, ErrorHandlingPolicy policy)
    {
        _inner = inner;
        _policy = policy;
    }
    
    public async Task<IEnumerable<TestDefinition>> BuildTestsAsync(TestMetadata metadata, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _inner.BuildTestsAsync(metadata, cancellationToken);
        }
        catch (Exception ex) when (_policy.ContinueOnError)
        {
            if (_policy.LogErrors)
            {
                Console.Error.WriteLine($"[TestBuilder] Error building tests for {metadata.TestMethod.Name}: {ex.Message}");
            }
            
            return _policy.CollectPartialResults 
                ? Array.Empty<TestDefinition>() 
                : throw;
        }
    }
}

/// <summary>
/// Decorator that adds timeout enforcement to TestBuilder.
/// </summary>
internal class TimeoutTestBuilderDecorator : ITestBuilderInternal
{
    private readonly ITestBuilderInternal _inner;
    private readonly TimeSpan _timeout;
    
    public TimeoutTestBuilderDecorator(ITestBuilderInternal inner, TimeSpan timeout)
    {
        _inner = inner;
        _timeout = timeout;
    }
    
    public async Task<IEnumerable<TestDefinition>> BuildTestsAsync(TestMetadata metadata, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);
        
        try
        {
            return await _inner.BuildTestsAsync(metadata, cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TestBuilderException($"Test building timed out after {_timeout.TotalSeconds} seconds")
            {
                TestMetadata = metadata
            };
        }
    }
}

/// <summary>
/// Decorator that adds caching to TestBuilder.
/// </summary>
internal class CachingTestBuilderDecorator : ITestBuilderInternal
{
    private readonly ITestBuilderInternal _inner;
    private readonly Dictionary<string, List<TestDefinition>> _cache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    
    public CachingTestBuilderDecorator(ITestBuilderInternal inner)
    {
        _inner = inner;
    }
    
    public async Task<IEnumerable<TestDefinition>> BuildTestsAsync(TestMetadata metadata, CancellationToken cancellationToken = default)
    {
        // Generate cache key from metadata
        var cacheKey = GenerateCacheKey(metadata);
        
        // Try to get from cache
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }
        }
        finally
        {
            _cacheLock.Release();
        }
        
        // Build and cache
        var definitions = (await _inner.BuildTestsAsync(metadata, cancellationToken)).ToList();
        
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            _cache[cacheKey] = definitions;
        }
        finally
        {
            _cacheLock.Release();
        }
        
        return definitions;
    }
    
    private static string GenerateCacheKey(TestMetadata metadata)
    {
        // Simple cache key - in production would need more sophisticated approach
        return $"{metadata.TestClassType.FullName}.{metadata.TestMethod.Name}#{metadata.GetHashCode()}";
    }
}

/// <summary>
/// Extension methods for configuring TestBuilder with dependency injection.
/// </summary>
public static class TestBuilderServiceCollectionExtensions
{
    /// <summary>
    /// Adds TestBuilder services to the service collection.
    /// </summary>
    public static IServiceCollection AddTestBuilder(this IServiceCollection services, Action<TestBuilderConfiguration>? configure = null)
    {
        // Add configuration
        services.AddSingleton(sp =>
        {
            var config = new TestBuilderConfiguration();
            configure?.Invoke(config);
            return config;
        });
        
        // Add telemetry
        services.AddSingleton<TestBuilderTelemetry>();
        
        // Add factory
        services.AddSingleton<ITestBuilderFactory, DefaultTestBuilderFactory>();
        
        // Add builder as transient (new instance per request)
        services.AddTransient<ITestBuilderInternal>(sp =>
        {
            var factory = sp.GetRequiredService<ITestBuilderFactory>();
            return factory.Create(sp);
        });
        
        return services;
    }
    
    /// <summary>
    /// Adds a custom TestBuilder decorator.
    /// </summary>
    public static IServiceCollection AddTestBuilderDecorator<TDecorator>(this IServiceCollection services)
        where TDecorator : class, ITestBuilderDecorator
    {
        services.AddTransient<ITestBuilderDecorator, TDecorator>();
        return services;
    }
}