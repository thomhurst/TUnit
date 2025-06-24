using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.Engine;

/// <summary>
/// Implementation of data source resolver
/// </summary>
public class DataSourceResolver : IDataSourceResolver
{
    public async Task<IEnumerable<object?[]>> ResolveDataSource(TestDataSource dataSource)
    {
        return await ResolveDataAsync(dataSource);
    }
    
    public async Task<IEnumerable<object?[]>> ResolveDataAsync(TestDataSource dataSource)
    {
        if (dataSource is StaticTestDataSource staticSource)
        {
            return await Task.FromResult(staticSource.GetData());
        }
        
        if (dataSource is DynamicTestDataSource dynamicSource)
        {
            return await ResolveDynamicDataAsync(dynamicSource);
        }
        
        throw new NotSupportedException($"Unsupported data source type: {dataSource.GetType().Name}");
    }
    
    public async Task<object?> ResolvePropertyDataAsync(PropertyDataSource propertyDataSource)
    {
        var data = await ResolveDataAsync(propertyDataSource.DataSource);
        var firstSet = data.FirstOrDefault();
        return firstSet?.FirstOrDefault();
    }
    
    private async Task<IEnumerable<object?[]>> ResolveDynamicDataAsync(DynamicTestDataSource dynamicSource)
    {
        // Create instance if needed
        object? instance = null;
        if (!dynamicSource.IsShared)
        {
            #pragma warning disable IL2072 // Dynamic data source types are specified by user
            instance = Activator.CreateInstance(dynamicSource.SourceType);
            #pragma warning restore IL2072
        }
        
        // Find the member
        #pragma warning disable IL2075 // Dynamic data source types are specified by user
        var member = dynamicSource.SourceType.GetMember(
            dynamicSource.SourceMemberName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
            .FirstOrDefault();
        #pragma warning restore IL2075
        
        if (member == null)
        {
            throw new InvalidOperationException($"Could not find member '{dynamicSource.SourceMemberName}' on type '{dynamicSource.SourceType.FullName}'");
        }
        
        object? rawData = member switch
        {
            PropertyInfo property => property.GetValue(instance),
            MethodInfo method => method.Invoke(instance, Array.Empty<object>()),
            FieldInfo field => field.GetValue(instance),
            _ => throw new InvalidOperationException($"Unsupported member type: {member.GetType().Name}")
        };
        
        // Handle async results
        if (rawData is Task task)
        {
            await task;
            #pragma warning disable IL2075 // Task<T> types are known
            var resultProperty = task.GetType().GetProperty("Result");
            #pragma warning restore IL2075
            rawData = resultProperty?.GetValue(task);
        }
        
        // Convert to object?[][]
        if (rawData is IEnumerable<object?[]> objectArrays)
        {
            return objectArrays;
        }
        
        if (rawData is System.Collections.IEnumerable enumerable)
        {
            var result = new List<object?[]>();
            foreach (var item in enumerable)
            {
                if (item is object?[] array)
                {
                    result.Add(array);
                }
                else
                {
                    result.Add(new[] { item });
                }
            }
            return result;
        }
        
        throw new InvalidOperationException($"Data source '{dynamicSource.SourceMemberName}' did not return a valid collection");
    }
}