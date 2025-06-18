using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Helpers;

public static class AsyncDataSourceHelper
{
    public static async IAsyncEnumerable<Func<Task<object?[]?>>> WrapAsyncEnumerable(
        IAsyncDataSourceGeneratorAttribute asyncDataSourceGenerator,
        DataGeneratorMetadata dataGeneratorMetadata,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (asyncDataSourceGenerator is IDataAttribute generatorObj)
        {
            await DataSourceInitializer.InitializeAsync(
                generatorObj,
                dataGeneratorMetadata,
                dataGeneratorMetadata.TestBuilderContext,
                obj => { }
            ).ConfigureAwait(false);
            await ObjectInitializer.InitializeAsync(generatorObj).ConfigureAwait(false);
        }

        var asyncEnumerable = asyncDataSourceGenerator.GenerateAsync(dataGeneratorMetadata);

        await foreach (var func in asyncEnumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return async () =>
            {
                var result = await func().ConfigureAwait(false);
                if (result == null)
                {
                    return result;
                }
                foreach (var t in result)
                {
                    if (t != null)
                    {
                        await DataSourceInitializer.InitializeAsync(
                            t,
                            dataGeneratorMetadata,
                            dataGeneratorMetadata.TestBuilderContext,
                            obj => { }
                        ).ConfigureAwait(false);
                    }
                }
                return result;
            };
        }
    }

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

    private static async IAsyncEnumerable<Func<Task<object?[]?>>> EmptyAsyncEnumerable()
    {
        await Task.CompletedTask;
        yield break;
    }

#pragma warning disable CS1998
    private static async IAsyncEnumerable<Func<Task<object?[]?>>> CreateFromArguments(ArgumentsAttribute argumentsAttribute)
#pragma warning restore CS1998
    {
        yield return async () => await Task.FromResult(argumentsAttribute.Values).ConfigureAwait(false);
    }

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

    public static async Task<object?> GetFirstValueAsync(
        IAsyncEnumerable<Func<Task<object?[]?>>> asyncEnumerable,
        CancellationToken cancellationToken = default)
    {
        await foreach (var func in asyncEnumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var result = await func().ConfigureAwait(false);
            var value = result?.ElementAtOrDefault(0);
            return value;
        }
        return null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.")]
    private static async Task InitializeObjectGraphAsync(object obj, HashSet<object>? visited = null)
    {
        visited ??= new HashSet<object>();
        if (!visited.Add(obj))
        {
            return;
        }
        var objType = obj.GetType();
        if (objType.IsPrimitive || obj is string || objType.IsEnum)
        {
            return;
        }
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
                continue;
            }
        }
        if (obj is IAsyncInitializer)
        {
            await ObjectInitializer.InitializeAsync(obj).ConfigureAwait(false);
        }
    }
}
