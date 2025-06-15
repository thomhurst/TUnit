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
        if (asyncDataSourceGenerator is object generatorObj)
        {
            // Use DataSourceInitializer to populate any [ClassDataSource] properties
            await DataSourceInitializer.InitializeAsync(
                generatorObj,
                dataGeneratorMetadata,
                dataGeneratorMetadata.TestBuilderContext,
                obj => { /* Object registration handled elsewhere */ }
            ).ConfigureAwait(false);
        }

        var asyncEnumerable = asyncDataSourceGenerator.GenerateAsync(dataGeneratorMetadata);
        
        await foreach (var func in asyncEnumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return func;
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
            
            // Initialize the value if it implements IAsyncInitializer
            if (value != null)
            {
                await ObjectInitializer.InitializeAsync(value, cancellationToken).ConfigureAwait(false);
            }
            
            return value;
        }
        
        return null;
    }
    
}