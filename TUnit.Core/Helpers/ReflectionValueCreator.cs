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
            IDataSourceGeneratorAttribute dataSourceGeneratorAttribute => dataSourceGeneratorAttribute.Generate(dataGeneratorMetadata).ElementAtOrDefault(0)?.Invoke()?.ElementAtOrDefault(0),
            MethodDataSourceAttribute methodDataSourceAttribute => (methodDataSourceAttribute.ClassProvidingDataSource ?? classInformation.Type).GetMethod(
                methodDataSourceAttribute.MethodNameProvidingDataSource, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy) !.Invoke(null,
                methodDataSourceAttribute.Arguments),
            NoOpDataAttribute => null,
            _ => throw new ArgumentOutOfRangeException(nameof(generator), generator, null)
        };
}
