using System.Reflection;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Helpers;

internal static class ReflectionValueCreator
{
    // Synchronous version for backward compatibility
    public static object? CreatePropertyValue(SourceGeneratedClassInformation classInformation,
        TestBuilderContextAccessor testBuilderContextAccessor,
        IDataAttribute generator,
        SourceGeneratedPropertyInformation property,
        DataGeneratorMetadata dataGeneratorMetadata)
    {
        return Task.Run(async () => await CreatePropertyValueAsync(classInformation, testBuilderContextAccessor, generator, property, dataGeneratorMetadata).ConfigureAwait(false)).GetAwaiter().GetResult();
    }

    public static async Task<object?> CreatePropertyValueAsync(SourceGeneratedClassInformation classInformation,
        TestBuilderContextAccessor testBuilderContextAccessor,
        IDataAttribute generator,
        SourceGeneratedPropertyInformation property,
        DataGeneratorMetadata dataGeneratorMetadata) =>
        generator switch
        {
            ArgumentsAttribute argumentsAttribute => argumentsAttribute.Values.ElementAtOrDefault(0),
            ClassConstructorAttribute classConstructorAttribute => ((IClassConstructor) Activator.CreateInstance(classConstructorAttribute.ClassConstructorType)!).Create(
                property.Type, new ClassConstructorMetadata
                {
                    TestBuilderContext = testBuilderContextAccessor.Current,
                    TestSessionId = string.Empty
                }),
            IAsyncDataSourceGeneratorAttribute asyncDataSourceGeneratorAttribute => await GetFirstAsyncValueWithInitAsync(asyncDataSourceGeneratorAttribute, dataGeneratorMetadata).ConfigureAwait(false),
            IDataSourceGeneratorAttribute dataSourceGeneratorAttribute => await GetFirstValueWithInitAsync(dataSourceGeneratorAttribute, dataGeneratorMetadata).ConfigureAwait(false),
            MethodDataSourceAttribute methodDataSourceAttribute => (methodDataSourceAttribute.ClassProvidingDataSource ?? classInformation.Type).GetMethod(
                methodDataSourceAttribute.MethodNameProvidingDataSource, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy) !.Invoke(null,
                methodDataSourceAttribute.Arguments),
            NoOpDataAttribute => null,
            _ => throw new ArgumentOutOfRangeException(nameof(generator), generator, null)
        };
    
    private static async Task<object?> GetFirstValueWithInitAsync(IDataSourceGeneratorAttribute dataSourceGeneratorAttribute, DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Initialize the data generator if it implements IAsyncInitializer
        if (dataSourceGeneratorAttribute is IAsyncInitializer asyncInitializer)
        {
            await asyncInitializer.InitializeAsync().ConfigureAwait(false);
        }
        
        return dataSourceGeneratorAttribute.Generate(dataGeneratorMetadata).ElementAtOrDefault(0)?.Invoke()?.ElementAtOrDefault(0);
    }
    
    private static async Task<object?> GetFirstAsyncValueWithInitAsync(IAsyncDataSourceGeneratorAttribute asyncDataSourceGeneratorAttribute, DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Initialize the data generator if it implements IAsyncInitializer
        if (asyncDataSourceGeneratorAttribute is IAsyncInitializer asyncInitializer)
        {
            await asyncInitializer.InitializeAsync().ConfigureAwait(false);
        }
        
        return await GetFirstAsyncValueAsync(asyncDataSourceGeneratorAttribute.GenerateAsync(dataGeneratorMetadata)).ConfigureAwait(false);
    }
    
    private static async Task<object?> GetFirstAsyncValueAsync(IAsyncEnumerable<Func<Task<object?[]?>>> asyncEnumerable)
    {
        var enumerator = asyncEnumerable.GetAsyncEnumerator();
        try
        {
            if (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                var func = enumerator.Current;
                var task = func();
                var result = await task.ConfigureAwait(false);
                return result?.ElementAtOrDefault(0);
            }
            return null;
        }
        finally
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
        }
    }
}
