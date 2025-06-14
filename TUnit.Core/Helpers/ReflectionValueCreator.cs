using System.Reflection;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Helpers;

internal static class ReflectionValueCreator
{
    public static object? CreatePropertyValue(SourceGeneratedClassInformation classInformation,
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
            IAsyncDataSourceGeneratorAttribute asyncDataSourceGeneratorAttribute => GetFirstAsyncValueWithInit(asyncDataSourceGeneratorAttribute, dataGeneratorMetadata),
            IDataSourceGeneratorAttribute dataSourceGeneratorAttribute => GetFirstValueWithInit(dataSourceGeneratorAttribute, dataGeneratorMetadata),
            MethodDataSourceAttribute methodDataSourceAttribute => (methodDataSourceAttribute.ClassProvidingDataSource ?? classInformation.Type).GetMethod(
                methodDataSourceAttribute.MethodNameProvidingDataSource, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy) !.Invoke(null,
                methodDataSourceAttribute.Arguments),
            NoOpDataAttribute => null,
            _ => throw new ArgumentOutOfRangeException(nameof(generator), generator, null)
        };
    
    private static object? GetFirstValueWithInit(IDataSourceGeneratorAttribute dataSourceGeneratorAttribute, DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Initialize the data generator if it implements IAsyncInitializer
        if (dataSourceGeneratorAttribute is IAsyncInitializer asyncInitializer)
        {
            asyncInitializer.InitializeAsync().GetAwaiter().GetResult();
        }
        
        return dataSourceGeneratorAttribute.Generate(dataGeneratorMetadata).ElementAtOrDefault(0)?.Invoke()?.ElementAtOrDefault(0);
    }
    
    private static object? GetFirstAsyncValueWithInit(IAsyncDataSourceGeneratorAttribute asyncDataSourceGeneratorAttribute, DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Initialize the data generator if it implements IAsyncInitializer
        if (asyncDataSourceGeneratorAttribute is IAsyncInitializer asyncInitializer)
        {
            asyncInitializer.InitializeAsync().GetAwaiter().GetResult();
        }
        
        return GetFirstAsyncValue(asyncDataSourceGeneratorAttribute.GenerateAsync(dataGeneratorMetadata));
    }
    
    private static object? GetFirstAsyncValue(IAsyncEnumerable<Func<Task<object?[]?>>> asyncEnumerable)
    {
        var enumerator = asyncEnumerable.GetAsyncEnumerator();
        try
        {
            if (enumerator.MoveNextAsync().GetAwaiter().GetResult())
            {
                var func = enumerator.Current;
                var task = func();
                var result = task.GetAwaiter().GetResult();
                return result?.ElementAtOrDefault(0);
            }
            return null;
        }
        finally
        {
            enumerator.DisposeAsync().GetAwaiter().GetResult();
        }
    }
}
