using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Enums;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Helpers;

/// <summary>
/// Provides centralized logic for initializing data sources and their properties.
/// Uses a two-pass approach:
/// 1. First pass: Populate all data sources (IAsyncDataSourceGeneratorAttribute)
/// 2. Second pass: Initialize objects marked with IRequiresImmediateInitialization
/// </summary>
[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.")]
[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
[UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.")]
[UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.")]
internal static class DataSourceInitializer
{
    /// <summary>
    /// Main entry point - initializes an object instance using metadata from a test method
    /// </summary>
    public static async Task InitializeAsync(object instance, MethodMetadata methodMetadata)
    {
        var context = new InitializationContext();

        try
        {
            // Pass 1: Populate all data sources
            await PopulateDataSourcesAsync(instance, methodMetadata.Class, context);

            // Pass 2: Initialize IRequiresImmediateInitialization objects in dependency order
            await InitializeDependenciesAsync(instance, methodMetadata.Class, context);
        }
        catch (Exception ex) when (ex is not DataSourceInitializationException)
        {
            throw new DataSourceInitializationException(
                $"Failed to initialize data sources for {methodMetadata.Class.Type.Name}.{methodMetadata.Name}", ex);
        }
    }

    /// <summary>
    /// Pass 1: Populates all properties that have IAsyncDataSourceGeneratorAttribute
    /// </summary>
    private static async Task PopulateDataSourcesAsync(object instance, ClassMetadata classMetadata, InitializationContext context)
    {
        if (!context.VisitedForDataSources.Add(instance))
        {
            return;
        }

        foreach (var property in classMetadata.Properties)
        {
            try
            {
                var propertyValue = property.Getter(instance);

                // If property is null and has a data source attribute, generate the value
                if (propertyValue is null)
                {
                    var dataAttribute = property.Attributes
                        .FirstOrDefault(x => x.AttributeType.IsAssignableTo(typeof(IAsyncDataSourceGeneratorAttribute)));

                    if (dataAttribute is not null)
                    {
                        propertyValue = await GeneratePropertyValueAsync(instance, dataAttribute, property, context);

                        if (propertyValue is not null && property.ReflectionInfo.CanWrite)
                        {
                            property.ReflectionInfo.SetValue(instance, propertyValue);
                        }
                    }
                }

                // Recursively populate data sources for nested objects
                if (propertyValue is not null && property.ClassMetadata is not null)
                {
                    await PopulateDataSourcesAsync(propertyValue, property.ClassMetadata, context);
                }
            }
            catch (Exception ex)
            {
                throw new DataSourceInitializationException(
                    $"Failed to populate data source for property {classMetadata.Type.Name}.{property.Name}", ex);
            }
        }
    }

    /// <summary>
    /// Pass 2: Initializes objects that implement IRequiresImmediateInitialization
    /// </summary>
    private static async Task InitializeDependenciesAsync(object instance, ClassMetadata classMetadata, InitializationContext context)
    {
        var state = context.GetInitializationState(instance);

        switch (state)
        {
            case InitializationState.Initialized:
                return;
            case InitializationState.Initializing:
                throw new DataSourceInitializationException(
                    $"Circular initialization dependency detected for object of type {instance.GetType().Name}");
        }

        context.SetInitializationState(instance, InitializationState.Initializing);

        try
        {
            // First, initialize all nested IRequiresImmediateInitialization properties
            foreach (var property in classMetadata.Properties)
            {
                var propertyValue = property.Getter(instance);

                if (propertyValue is IRequiresImmediateInitialization && property.ClassMetadata is not null)
                {
                    await InitializeDependenciesAsync(propertyValue, property.ClassMetadata, context);
                }
            }

            // Since IRequiresImmediateInitialization is just a marker interface,
            // the initialization is complete once all nested dependencies are initialized
            context.SetInitializationState(instance, InitializationState.Initialized);
        }
        catch (Exception ex) when (ex is not DataSourceInitializationException)
        {
            throw new DataSourceInitializationException(
                $"Failed to initialize dependencies for object of type {instance.GetType().Name}", ex);
        }
    }

    /// <summary>
    /// Generates a property value using IAsyncDataSourceGeneratorAttribute
    /// </summary>
    private static async Task<object?> GeneratePropertyValueAsync(
        object instance,
        AttributeMetadata attributeMetadata,
        PropertyMetadata propertyMetadata,
        InitializationContext context)
    {
        var dataAttribute = (IAsyncDataSourceGeneratorAttribute)attributeMetadata.Instance;

        // Initialize the attribute itself if it requires it
        if (dataAttribute is IRequiresImmediateInitialization && attributeMetadata.ClassMetadata is not null)
        {
            await InitializeDependenciesAsync(dataAttribute, attributeMetadata.ClassMetadata, context);
        }

        // Create metadata for the generator
        var generatorMetadata = new DataGeneratorMetadata
        {
            Type = DataGeneratorType.Property,
            ClassInstanceArguments = [],
            TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext()),
            MembersToGenerate = [propertyMetadata],
            TestClassInstance = instance,
            TestInformation = null!, // This might need to be provided from context
            TestSessionId = Guid.NewGuid().ToString()
        };

        // Get the first generated value
        await using var asyncEnumerator = dataAttribute.GenerateAsync(generatorMetadata).GetAsyncEnumerator();

        if (await asyncEnumerator.MoveNextAsync())
        {
            var values = await asyncEnumerator.Current();
            return values?.ElementAtOrDefault(0);
        }

        return null;
    }

    /// <summary>
    /// Tracks initialization state to prevent circular dependencies
    /// </summary>
    private class InitializationContext
    {
        public HashSet<object> VisitedForDataSources { get; } = new(new ReferenceEqualityComparer());
        private readonly Dictionary<object, InitializationState> _initializationStates = new(new ReferenceEqualityComparer());

        public InitializationState GetInitializationState(object instance)
        {
            return _initializationStates.TryGetValue(instance, out var state)
                ? state
                : InitializationState.NotInitialized;
        }

        public void SetInitializationState(object instance, InitializationState state)
        {
            _initializationStates[instance] = state;
        }
    }

    private enum InitializationState
    {
        NotInitialized,
        Initializing,
        Initialized
    }
}

/// <summary>
/// Exception thrown when data source initialization fails
/// </summary>
public class DataSourceInitializationException : Exception
{
    public DataSourceInitializationException(string message) : base(message) { }
    public DataSourceInitializationException(string message, Exception innerException) : base(message, innerException) { }
}
