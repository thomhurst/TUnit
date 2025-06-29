using TUnit.Core.Models;

namespace TUnit.Core.Services;

/// <summary>
/// Dispatcher that routes test execution to strongly typed factories for maximum performance.
/// Falls back to weakly typed factories when strongly typed ones are not available.
/// </summary>
public class StronglyTypedFactoryDispatcher
{
    private readonly SourceGeneratedTestRegistry _registry;

    public StronglyTypedFactoryDispatcher(SourceGeneratedTestRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Creates a test class instance using the most appropriate factory.
    /// Prefers strongly typed factories for performance, falls back to weakly typed.
    /// </summary>
    /// <param name="testVariation">The test variation containing instance creation data</param>
    /// <returns>Created test class instance</returns>
    public object CreateTestInstance(TestVariation testVariation)
    {
        var testId = testVariation.TestId;

        // Try strongly typed factory first
        if (_registry.HasStronglyTypedFactory(testId))
        {
            return CreateInstanceUsingStronglyTypedFactory(testVariation);
        }

        // Fall back to weakly typed factory
        return CreateInstanceUsingWeaklyTypedFactory(testVariation);
    }

    /// <summary>
    /// Invokes a test method using the most appropriate invoker.
    /// Prefers strongly typed invokers for performance, falls back to weakly typed.
    /// </summary>
    /// <param name="testVariation">The test variation containing method invocation data</param>
    /// <param name="instance">The test class instance</param>
    /// <returns>Task representing the async method invocation</returns>
    public async Task<object?> InvokeTestMethod(TestVariation testVariation, object instance)
    {
        var testId = testVariation.TestId;

        // Try strongly typed invoker first
        if (_registry.HasStronglyTypedFactory(testId))
        {
            return await InvokeMethodUsingStronglyTypedInvoker(testVariation, instance);
        }

        // Fall back to weakly typed invoker
        return await InvokeMethodUsingWeaklyTypedInvoker(testVariation, instance);
    }

    /// <summary>
    /// Creates an instance using strongly typed factories based on the test variation signature.
    /// </summary>
    private object CreateInstanceUsingStronglyTypedFactory(TestVariation testVariation)
    {
        var testId = testVariation.TestId;
        var classArgs = testVariation.ClassArguments ?? Array.Empty<object?>();

        // Try to get the appropriate strongly typed factory based on argument count
        if (classArgs.Length == 0)
        {
            // Parameterless constructor
            var factory = _registry.GetStronglyTypedClassFactory<Func<object>>(testId);
            if (factory != null)
            {
                return factory.DynamicInvoke()!;
            }
        }
        else
        {
            // Parameterized constructor - try to find matching factory
            var parameterizedTestId = $"{testId}_{classArgs.Length}args";
            var parameterizedFactory = _registry.GetStronglyTypedClassFactory<Delegate>(parameterizedTestId);
            if (parameterizedFactory != null)
            {
                return parameterizedFactory.DynamicInvoke(classArgs)!;
            }
        }

        throw new InvalidOperationException(
            $"No strongly typed class factory found for test {testId} with {classArgs.Length} arguments");
    }

    /// <summary>
    /// Creates an instance using weakly typed factories as fallback.
    /// </summary>
    private object CreateInstanceUsingWeaklyTypedFactory(TestVariation testVariation)
    {
        var testId = testVariation.TestId;
        var classArgs = testVariation.ClassArguments ?? Array.Empty<object?>();

        if (classArgs.Length == 0)
        {
            var factory = _registry.GetClassFactory(testId);
            if (factory != null)
            {
                return factory();
            }
        }
        else
        {
            var parameterizedFactory = _registry.GetParameterizedClassFactory(testId);
            if (parameterizedFactory != null)
            {
                return parameterizedFactory(classArgs);
            }
        }

        throw new InvalidOperationException(
            $"No class factory found for test {testId}");
    }

    /// <summary>
    /// Invokes a method using strongly typed invokers based on the test variation signature.
    /// </summary>
    private async Task<object?> InvokeMethodUsingStronglyTypedInvoker(TestVariation testVariation, object instance)
    {
        var testId = testVariation.TestId;
        var methodArgs = testVariation.MethodArguments ?? Array.Empty<object?>();

        // Get the strongly typed method invoker
        var invoker = _registry.GetStronglyTypedMethodInvoker<Delegate>(testId);
        if (invoker == null)
        {
            throw new InvalidOperationException(
                $"No strongly typed method invoker found for test {testId}");
        }

        // Prepare arguments: instance first, then method arguments
        var allArgs = new object?[methodArgs.Length + 1];
        allArgs[0] = instance;
        Array.Copy(methodArgs, 0, allArgs, 1, methodArgs.Length);

        // Invoke the strongly typed method
        var result = invoker.DynamicInvoke(allArgs);

        // Handle async results
        if (result is Task task)
        {
            await task;

            // Extract result from Task<T> if present
            var taskType = task.GetType();
            if (taskType.IsGenericType && taskType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                // Get Result property in an AOT-safe way
#pragma warning disable IL2075 // Task<T> type is known
                var resultProperty = taskType.GetProperty("Result");
#pragma warning restore IL2075
                if (resultProperty != null)
                {
                    try
                    {
                        return resultProperty.GetValue(task);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }

            return null; // Task (void)
        }

        return result;
    }

    /// <summary>
    /// Invokes a method using weakly typed invokers as fallback.
    /// </summary>
    private async Task<object?> InvokeMethodUsingWeaklyTypedInvoker(TestVariation testVariation, object instance)
    {
        var testId = testVariation.TestId;
        var methodArgs = testVariation.MethodArguments ?? Array.Empty<object?>();

        var invoker = _registry.GetMethodInvoker(testId);
        if (invoker == null)
        {
            throw new InvalidOperationException(
                $"No method invoker found for test {testId}");
        }

        return await invoker(instance, methodArgs);
    }

    /// <summary>
    /// Sets properties on a test instance using available property setters.
    /// </summary>
    /// <param name="testVariation">The test variation containing property data</param>
    /// <param name="instance">The test class instance</param>
    public void SetInstanceProperties(TestVariation testVariation, object instance)
    {
        if (testVariation.PropertyValues == null || testVariation.PropertyValues.Count == 0)
        {
            return;
        }

        var testId = testVariation.TestId;
        var propertySetters = _registry.GetPropertySetters(testId);

        foreach (var (propertyName, propertyValue) in testVariation.PropertyValues)
        {
            if (propertySetters.TryGetValue(propertyName, out var setter))
            {
                setter(instance, propertyValue);
            }
        }
    }

    /// <summary>
    /// Gets performance statistics about factory usage.
    /// </summary>
    /// <returns>Performance statistics</returns>
    public FactoryDispatcherStats GetPerformanceStats()
    {
        var allTestIds = _registry.GetRegisteredTestIds();
        var stronglyTypedCount = 0;
        var weaklyTypedCount = 0;

        foreach (var testId in allTestIds)
        {
            if (_registry.HasStronglyTypedFactory(testId))
            {
                stronglyTypedCount++;
            }
            else
            {
                weaklyTypedCount++;
            }
        }

        return new FactoryDispatcherStats
        {
            TotalTests = allTestIds.Count,
            StronglyTypedTests = stronglyTypedCount,
            WeaklyTypedTests = weaklyTypedCount,
            PerformanceRatio = allTestIds.Count > 0 ? (double) stronglyTypedCount / allTestIds.Count : 0.0
        };
    }
}

/// <summary>
/// Performance statistics for the factory dispatcher.
/// </summary>
public sealed class FactoryDispatcherStats
{
    /// <summary>
    /// Total number of registered tests.
    /// </summary>
    public int TotalTests { get; init; }

    /// <summary>
    /// Number of tests using strongly typed factories.
    /// </summary>
    public int StronglyTypedTests { get; init; }

    /// <summary>
    /// Number of tests using weakly typed factories.
    /// </summary>
    public int WeaklyTypedTests { get; init; }

    /// <summary>
    /// Ratio of strongly typed tests (0.0 to 1.0, higher is better for performance).
    /// </summary>
    public double PerformanceRatio { get; init; }

    /// <summary>
    /// Gets a human-readable description of the performance characteristics.
    /// </summary>
    public string Description
    {
        get
        {
            var percentage = (PerformanceRatio * 100).ToString("F1");
            return $"{StronglyTypedTests}/{TotalTests} tests ({percentage}%) using strongly typed factories for optimal performance";
        }
    }
}
