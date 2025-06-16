using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Helpers;

/// <summary>
/// Provides helper methods for working with async data sources without blocking threads.
/// </summary>
public static class AsyncDataSourceHelper
{

    /// <summary>
    /// Wraps an async data source generator, ensuring proper initialization.
    /// </summary>
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> WrapAsyncEnumerable(
        IAsyncDataSourceGeneratorAttribute asyncDataSourceGenerator,
        DataGeneratorMetadata dataGeneratorMetadata,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // If the generator has properties with data sources, populate them first
        if (asyncDataSourceGenerator is IDataAttribute generatorObj)
        {
            // Use DataSourceInitializer to populate any [ClassDataSource] properties
            await DataSourceInitializer.InitializeAsync(
                generatorObj,
                dataGeneratorMetadata,
                dataGeneratorMetadata.TestBuilderContext,
                obj => { /* Object registration handled elsewhere */ }
            ).ConfigureAwait(false);
            
            // Initialize the data attribute itself since it may need to prepare data
            await ObjectInitializer.InitializeAsync(generatorObj).ConfigureAwait(false);
        }

        var asyncEnumerable = asyncDataSourceGenerator.GenerateAsync(dataGeneratorMetadata);

        await foreach (var func in asyncEnumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            // Wrap the function to ensure returned objects are properly initialized
            yield return async () =>
            {
                var result = await func().ConfigureAwait(false);

                // Initialize each object in the result array
                if (result == null)
                {
                    return result;
                }

                // Populate all data source properties for all objects
                foreach (var t in result)
                {
                    if (t != null)
                    {
                        // Initialize nested data source properties
                        await DataSourceInitializer.InitializeAsync(
                            t,
                            dataGeneratorMetadata,
                            dataGeneratorMetadata.TestBuilderContext,
                            obj => { /* Object registration handled elsewhere */ }
                        ).ConfigureAwait(false);
                    }
                }
                
                // Don't initialize IAsyncInitializer objects here - that should happen
                // during test execution when TestContext is available

                return result;
            };
        }
    }

    /// <summary>
    /// Creates an async enumerable from any data attribute.
    /// </summary>
    internal static IAsyncEnumerable<Func<Task<object?[]?>>> CreateAsyncEnumerable(
        IDataAttribute dataAttribute,
        DataGeneratorMetadata dataGeneratorMetadata,
        CancellationToken cancellationToken = default)
    {
        return dataAttribute switch
        {
            IAsyncDataSourceGeneratorAttribute asyncGen => WrapAsyncEnumerable(asyncGen, dataGeneratorMetadata, cancellationToken),
            ArgumentsAttribute args => CreateFromArguments(args),
            _ => EmptyAsyncEnumerable()
        };
    }

    /// <summary>
    /// Creates an empty async enumerable.
    /// </summary>
    private static async IAsyncEnumerable<Func<Task<object?[]?>>> EmptyAsyncEnumerable()
    {
        await Task.CompletedTask;
        yield break;
    }

    /// <summary>
    /// Creates an async enumerable from ArgumentsAttribute.
    /// </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private static async IAsyncEnumerable<Func<Task<object?[]?>>> CreateFromArguments(ArgumentsAttribute argumentsAttribute)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        yield return async () => await Task.FromResult(argumentsAttribute.Values).ConfigureAwait(false);
    }

    /// <summary>
    /// Enumerates an async enumerable and collects results into a list.
    /// This should be used sparingly and only when absolutely necessary.
    /// </summary>
    public static async Task<List<Func<Task<object?[]?>>>> ToListAsync(
        IAsyncEnumerable<Func<Task<object?[]?>>> asyncEnumerable,
        CancellationToken cancellationToken = default)
    {
        var results = new List<Func<Task<object?[]?>>>();

        await foreach (var item in asyncEnumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            results.Add(item);
        }

        return results;
    }

    /// <summary>
    /// Gets the first value from an async enumerable of functions.
    /// </summary>
    public static async Task<object?> GetFirstValueAsync(
        IAsyncEnumerable<Func<Task<object?[]?>>> asyncEnumerable,
        CancellationToken cancellationToken = default)
    {
        await foreach (var func in asyncEnumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var result = await func().ConfigureAwait(false);
            var value = result?.ElementAtOrDefault(0);

            // Don't initialize here - let the caller handle initialization
            // after all nested properties are populated
            return value;
        }

        return null;
    }

    /// <summary>
    /// Initializes an object graph in the correct order (children before parents).
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2075:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.")]
    private static async Task InitializeObjectGraphAsync(object obj, HashSet<object>? visited = null)
    {
        visited ??= new HashSet<object>();
        
        if (!visited.Add(obj))
        {
            return; // Already processed
        }

        var objType = obj.GetType();
        
        // Skip primitive types and strings
        if (objType.IsPrimitive || obj is string || objType.IsEnum)
        {
            return;
        }

        // First, recursively initialize all property values
        var properties = objType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(obj);
                if (value != null && !property.PropertyType.IsPrimitive && !(value is string) && !property.PropertyType.IsEnum)
                {
                    await InitializeObjectGraphAsync(value, visited).ConfigureAwait(false);
                }
            }
            catch
            {
                // Skip properties that throw when accessed
                continue;
            }
        }

        // Then initialize this object if it implements IAsyncInitializer
        if (obj is IAsyncInitializer)
        {
            await ObjectInitializer.InitializeAsync(obj).ConfigureAwait(false);
        }
    }

}
