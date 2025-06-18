using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Enums;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Helpers;

/// <summary>
/// Provides centralized logic for initializing data sources and their properties.
/// This follows the DRY principle by consolidating initialization logic used across the framework.
/// </summary>
[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.")]
[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
[UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.")]
[UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.")]
internal static class DataSourceInitializer
{
    public static async Task InitializeAsync(object instance, MethodMetadata methodMetadata)
    {
        await InitializeAsync(instance, methodMetadata.Class, []);
    }

    public static async Task InitializeAsync(object instance, ClassMetadata classMetadata, HashSet<object?> visited)
    {
        if (!visited.Add(instance))
        {
            return;
        }

        foreach (var property in classMetadata.Properties)
        {
            await InitializeAsync(instance, property, visited);
        }
    }

    public static async Task InitializeAsync(object instance, PropertyMetadata propertyMetadata, HashSet<object?> visited)
    {
        var propertyValue = propertyMetadata.Getter(instance);

        if (propertyValue is null)
        {
            var dataAttribute = propertyMetadata.TestAttributes.FirstOrDefault(x => x.AttributeType.IsAssignableTo(typeof(IAsyncDataSourceGeneratorAttribute)));

            if(dataAttribute is not null)
            {
                await InitializeAsync(instance, dataAttribute, propertyMetadata, visited);
            }
        }

        propertyValue ??= propertyMetadata.Getter(instance);

        if (propertyValue is IRequiresImmediateInitialization)
        {
            if (propertyMetadata.ClassMetadata is not null)
            {
                await InitializeAsync(propertyValue, propertyMetadata.ClassMetadata, visited);
            }
        }
    }

    public static async Task InitializeAsync(object instance, AttributeMetadata attributeMetadata, PropertyMetadata propertyMetadata, HashSet<object?> visited)
    {
        var dataAttribute = (IAsyncDataSourceGeneratorAttribute)attributeMetadata.Instance;

        if (attributeMetadata.ClassMetadata is not null)
        {
            await InitializeAsync(dataAttribute, attributeMetadata.ClassMetadata, visited);
        }

        var values = dataAttribute.GenerateAsync(new DataGeneratorMetadata
        {
            Type = DataGeneratorType.Property,
            ClassInstanceArguments = [],
            TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext()),
            MembersToGenerate = [propertyMetadata],
            TestClassInstance = instance,
            TestInformation = null!,
            TestSessionId = ""
        });

        await using var asyncEnumerator = values.GetAsyncEnumerator();

        await asyncEnumerator.MoveNextAsync();

        var array = await asyncEnumerator.Current();

        var value = array?.ElementAtOrDefault(0);

        if (value is not null)
        {
            propertyMetadata.ReflectionInfo.SetValue(instance, value);
        }
    }
}
