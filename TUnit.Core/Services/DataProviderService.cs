using System.Runtime.CompilerServices;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Services;

/// <summary>
/// Default implementation of data provider service.
/// </summary>
public class DataProviderService : IDataProviderService
{
    /// <inheritdoc />
    public async IAsyncEnumerable<object?[]> GetTestDataAsync(
        IEnumerable<IDataSourceProvider> providers,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var providerList = providers.ToList();
        
        // If no providers, yield empty array
        if (!providerList.Any())
        {
            yield return Array.Empty<object?>();
            yield break;
        }

        // Get data from all providers
        foreach (var provider in providerList)
        {
            if (provider.IsAsync)
            {
                await foreach (var data in provider.GetDataAsync())
                {
                    yield return data;
                }
            }
            else
            {
                foreach (var data in provider.GetData())
                {
                    yield return data;
                }
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<(object?[] classArgs, object?[] methodArgs)> GetTestDataCombinationsAsync(
        IEnumerable<IDataSourceProvider> classDataProviders,
        IEnumerable<IDataSourceProvider> methodDataProviders,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Collect all class data
        var classDataList = new List<object?[]>();
        await foreach (var classData in GetTestDataAsync(classDataProviders, cancellationToken))
        {
            classDataList.Add(classData);
        }

        // Collect all method data
        var methodDataList = new List<object?[]>();
        await foreach (var methodData in GetTestDataAsync(methodDataProviders, cancellationToken))
        {
            methodDataList.Add(methodData);
        }

        // If no data from providers, use empty arrays
        if (!classDataList.Any())
        {
            classDataList.Add(Array.Empty<object?>());
        }
        if (!methodDataList.Any())
        {
            methodDataList.Add(Array.Empty<object?>());
        }

        // Generate all combinations
        foreach (var classArgs in classDataList)
        {
            foreach (var methodArgs in methodDataList)
            {
                yield return (classArgs, methodArgs);
            }
        }
    }
}