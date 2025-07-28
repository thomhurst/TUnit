using System.Collections.Generic;
using System.Linq;
using TUnit.Core.Enums;

namespace TUnit.Core;

internal static class DataGeneratorMetadataCreator
{
    public static DataGeneratorMetadata CreateDataGeneratorMetadata(
        TestMetadata testMetadata,
        string testSessionId,
        DataGeneratorType generatorType,
        object? testClassInstance,
        object?[]? classInstanceArguments,
        TestBuilderContextAccessor contextAccessor)
    {
        // Determine which parameters we're generating for
        var parametersToGenerate = generatorType == DataGeneratorType.ClassParameters
            ? testMetadata.MethodMetadata.Class.Parameters
            : testMetadata.MethodMetadata.Parameters;

        // Filter out CancellationToken if it's the last parameter (handled by the engine)
        if (generatorType == DataGeneratorType.TestParameters && parametersToGenerate.Length > 0)
        {
            var lastParam = parametersToGenerate[parametersToGenerate.Length - 1];
            if (lastParam.Type == typeof(System.Threading.CancellationToken))
            {
                var newArray = new ParameterMetadata[parametersToGenerate.Length - 1];
                Array.Copy(parametersToGenerate, 0, newArray, 0, parametersToGenerate.Length - 1);
                parametersToGenerate = newArray;
            }
        }

        // Handle property data generation specifically
        MemberMetadata[] membersToGenerate;
        if (generatorType == DataGeneratorType.Property)
        {
            // For properties, we generate data for properties that have data sources
            // If PropertyDataSources is populated, use only those properties
            if (testMetadata.PropertyDataSources.Length > 0)
            {
                var propertyMetadataList = new List<PropertyMetadata>();
                var allProperties = testMetadata.MethodMetadata.Class.Properties;
                
                foreach (var propertyDataSource in testMetadata.PropertyDataSources)
                {
                    var matchingProperty = allProperties.FirstOrDefault(p => p.Name == propertyDataSource.PropertyName);
                    if (matchingProperty != null)
                    {
                        propertyMetadataList.Add(matchingProperty);
                    }
                }
                
                membersToGenerate = [.. propertyMetadataList];
            }
            else
            {
                // If no specific PropertyDataSources, include all class properties
                membersToGenerate = testMetadata.MethodMetadata.Class.Properties;
            }
        }
        else
        {
            // For parameters (class or test), use the parameter metadata
            membersToGenerate = [..parametersToGenerate];
        }

        return new DataGeneratorMetadata
        {
            TestBuilderContext = contextAccessor,
            MembersToGenerate = membersToGenerate,
            TestInformation = testMetadata.MethodMetadata,
            Type = generatorType,
            TestSessionId = testSessionId,
            TestClassInstance = testClassInstance,
            ClassInstanceArguments = classInstanceArguments
        };
    }
}
