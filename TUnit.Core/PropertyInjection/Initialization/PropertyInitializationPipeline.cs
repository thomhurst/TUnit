using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Core.PropertyInjection.Initialization.Strategies;

namespace TUnit.Core.PropertyInjection.Initialization;

/// <summary>
/// Defines and executes the property initialization pipeline.
/// Follows Pipeline pattern for clear, sequential processing steps.
/// </summary>
internal sealed class PropertyInitializationPipeline
{
    private readonly List<IPropertyInitializationStrategy> _strategies;
    private readonly List<Func<PropertyInitializationContext, Task>> _beforeSteps;
    private readonly List<Func<PropertyInitializationContext, Task>> _afterSteps;

    public PropertyInitializationPipeline()
    {
        _strategies = new List<IPropertyInitializationStrategy>
        {
            new SourceGeneratedPropertyStrategy(),
            new ReflectionPropertyStrategy(),
            new NestedPropertyStrategy()
        };

        _beforeSteps = new List<Func<PropertyInitializationContext, Task>>();
        _afterSteps = new List<Func<PropertyInitializationContext, Task>>();
    }

    /// <summary>
    /// Adds a step to execute before property initialization.
    /// </summary>
    public PropertyInitializationPipeline AddBeforeStep(Func<PropertyInitializationContext, Task> step)
    {
        _beforeSteps.Add(step);
        return this;
    }

    /// <summary>
    /// Adds a step to execute after property initialization.
    /// </summary>
    public PropertyInitializationPipeline AddAfterStep(Func<PropertyInitializationContext, Task> step)
    {
        _afterSteps.Add(step);
        return this;
    }

    /// <summary>
    /// Adds a custom strategy to the pipeline.
    /// </summary>
    public PropertyInitializationPipeline AddStrategy(IPropertyInitializationStrategy strategy)
    {
        _strategies.Add(strategy);
        return this;
    }

    /// <summary>
    /// Executes the pipeline for a given context.
    /// </summary>
    public async Task ExecuteAsync(PropertyInitializationContext context)
    {
        try
        {
            // Execute before steps
            foreach (var step in _beforeSteps)
            {
                await step(context);
            }

            // Find and execute the appropriate strategy
            var executed = false;
            foreach (var strategy in _strategies)
            {
                if (strategy.CanHandle(context))
                {
                    await strategy.InitializePropertyAsync(context);
                    executed = true;
                    break;
                }
            }

            if (!executed && !context.IsNestedProperty)
            {
                // No strategy could handle this property
                throw new InvalidOperationException(
                    $"No initialization strategy available for property '{context.PropertyName}' " +
                    $"of type '{context.PropertyType.Name}' on '{context.Instance.GetType().Name}'");
            }

            // Execute after steps
            foreach (var step in _afterSteps)
            {
                await step(context);
            }
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            throw new InvalidOperationException(
                $"Failed to initialize property '{context.PropertyName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Executes the pipeline for multiple contexts in parallel.
    /// </summary>
    public async Task ExecuteParallelAsync(IEnumerable<PropertyInitializationContext> contexts)
    {
        var tasks = contexts.Select(ExecuteAsync);
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Creates a default pipeline with standard steps.
    /// </summary>
    public static PropertyInitializationPipeline CreateDefault()
    {
        return new PropertyInitializationPipeline()
            .AddBeforeStep(ValidateContext)
            .AddAfterStep(FinalizeInitialization);
    }

    /// <summary>
    /// Validates the initialization context before processing.
    /// </summary>
    private static Task ValidateContext(PropertyInitializationContext context)
    {
        if (context.Instance == null)
        {
            throw new ArgumentNullException(nameof(context.Instance), "Instance cannot be null");
        }

        if (string.IsNullOrEmpty(context.PropertyName))
        {
            throw new ArgumentException("Property name cannot be empty", nameof(context.PropertyName));
        }

        if (context.PropertyType == null)
        {
            throw new ArgumentNullException(nameof(context.PropertyType), "Property type cannot be null");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Finalizes the initialization after property is set.
    /// </summary>
    private static Task FinalizeInitialization(PropertyInitializationContext context)
    {
        // Any final cleanup or verification can go here
        return Task.CompletedTask;
    }
}