using System.Runtime.CompilerServices;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Helpers;

/// <summary>
/// Provides helper methods for working with async data sources without blocking threads.
/// </summary>
internal static class AsyncDataSourceHelper
{
    /// <summary>
    /// Converts a synchronous data source generator to an async enumerable.
    /// </summary>
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> ToAsyncEnumerable(
        IDataSourceGeneratorAttribute dataSourceGenerator,
        DataGeneratorMetadata dataGeneratorMetadata,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Initialize the data generator if it implements IAsyncInitializer
        if (dataSourceGenerator is IAsyncInitializer asyncInitializer)
        {
            await asyncInitializer.InitializeAsync().ConfigureAwait(false);
        }

        var syncEnumerable = dataSourceGenerator.Generate(dataGeneratorMetadata);
        
        foreach (var func in syncEnumerable)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Convert synchronous function to async
            yield return async () => await Task.FromResult(func()).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Wraps an async data source generator, ensuring proper initialization.
    /// </summary>
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> WrapAsyncEnumerable(
        IAsyncDataSourceGeneratorAttribute asyncDataSourceGenerator,
        DataGeneratorMetadata dataGeneratorMetadata,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Initialize the data generator if it implements IAsyncInitializer
        if (asyncDataSourceGenerator is IAsyncInitializer asyncInitializer)
        {
            await asyncInitializer.InitializeAsync().ConfigureAwait(false);
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
    public static IAsyncEnumerable<Func<Task<object?[]?>>> CreateAsyncEnumerable(
        IDataAttribute dataAttribute,
        DataGeneratorMetadata dataGeneratorMetadata,
        CancellationToken cancellationToken = default)
    {
        return dataAttribute switch
        {
            IAsyncDataSourceGeneratorAttribute asyncGen => WrapAsyncEnumerable(asyncGen, dataGeneratorMetadata, cancellationToken),
            IDataSourceGeneratorAttribute syncGen => ToAsyncEnumerable(syncGen, dataGeneratorMetadata, cancellationToken),
            ArgumentsAttribute args => CreateFromArguments(args),
            _ => AsyncEnumerable.Empty<Func<Task<object?[]?>>>()
        };
    }

    /// <summary>
    /// Creates an async enumerable from ArgumentsAttribute.
    /// </summary>
    private static async IAsyncEnumerable<Func<Task<object?[]?>>> CreateFromArguments(ArgumentsAttribute argumentsAttribute)
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
            return result?.ElementAtOrDefault(0);
        }
        
        return null;
    }
}