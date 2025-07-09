using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Helpers;

/// <summary>
/// AOT-compatible helper for handling runtime data source generators
/// </summary>
public static class RuntimeDataSourceHelper
{
    /// <summary>
    /// Generates data combinations from runtime data source generator attributes
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Type information is preserved by source generation")]
    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "Type information is preserved by source generation")]
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Type information is preserved by source generation")]
    public static async IAsyncEnumerable<TestDataCombination> GenerateDataCombinationsAsync(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | 
                                    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors |
                                    DynamicallyAccessedMemberTypes.PublicProperties)] Type testClassType,
        string testMethodName, 
        Func<Attribute[]> attributeFactory)
    {
        var attributes = attributeFactory();
        var dataSourceGenerators = attributes.OfType<AsyncUntypedDataSourceGeneratorAttribute>().ToList();
        
        if (!dataSourceGenerators.Any())
        {
            // No runtime generators, yield single empty combination
            yield return new TestDataCombination();
            yield break;
        }
        
        // Get method info for parameter metadata
        var methodInfo = testClassType.GetMethod(testMethodName, 
            BindingFlags.Public | BindingFlags.NonPublic | 
            BindingFlags.Instance | BindingFlags.Static);
            
        if (methodInfo == null)
        {
            throw new InvalidOperationException($"Method {testMethodName} not found on type {testClassType.Name}");
        }
        
        // Create parameter metadata
        var parameters = methodInfo.GetParameters();
        var parameterMetadatas = parameters.Select(p => new ParameterMetadata(p.ParameterType)
        {
            Name = p.Name ?? string.Empty,
            TypeReference = new TypeReference { AssemblyQualifiedName = p.ParameterType.AssemblyQualifiedName },
            ReflectionInfo = p
        }).ToArray();
        
        // Create method metadata
        var classMetadata = new ClassMetadata
        {
            Name = testClassType.Name,
            Type = testClassType,
            TypeReference = new TypeReference { AssemblyQualifiedName = testClassType.AssemblyQualifiedName },
            Namespace = testClassType.Namespace,
            Assembly = new AssemblyMetadata { Name = testClassType.Assembly.GetName().Name ?? "Unknown" },
            Parameters = Array.Empty<ParameterMetadata>(), // Constructor parameters
            Properties = Array.Empty<PropertyMetadata>(),
            Parent = null
        };
        
        var methodMetadata = new MethodMetadata
        {
            Name = methodInfo.Name,
            Type = testClassType,
            Parameters = parameterMetadatas,
            GenericTypeCount = methodInfo.IsGenericMethodDefinition ? methodInfo.GetGenericArguments().Length : 0,
            Class = classMetadata,
            ReturnTypeReference = new TypeReference { AssemblyQualifiedName = methodInfo.ReturnType.AssemblyQualifiedName },
            ReturnType = methodInfo.ReturnType,
            TypeReference = new TypeReference { AssemblyQualifiedName = testClassType.AssemblyQualifiedName }
        };
        
        // Create data generator metadata
        var dataGeneratorMetadata = new DataGeneratorMetadata
        {
            Type = DataGeneratorType.TestParameters,
            TestInformation = methodMetadata,
            TestClassInstance = null,
            MembersToGenerate = parameterMetadatas,
            TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext()),
            TestSessionId = Guid.NewGuid().ToString(),
            ClassInstanceArguments = null
        };
        
        // Generate data from each generator
        foreach (var generator in dataSourceGenerators)
        {
            await foreach (var dataFunc in generator.GenerateAsync(dataGeneratorMetadata))
            {
                var data = await dataFunc();
                if (data != null)
                {
                    yield return new TestDataCombination
                    {
                        MethodData = data,
                        ClassData = Array.Empty<object?>(),
                        PropertyValues = new Dictionary<string, object?>()
                    };
                }
            }
        }
    }
}