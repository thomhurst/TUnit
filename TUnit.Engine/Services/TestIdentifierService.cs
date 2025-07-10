using System.Linq;
using TUnit.Core;
using TUnit.Core.Data;

namespace TUnit.Engine.Services;

internal static class TestIdentifierService
{
    public static string GenerateTestId(TestMetadata metadata, TestDataCombination combination)
    {
        var methodMetadata = metadata.MethodMetadata;
        var classMetadata = methodMetadata.Class;
        
        var constructorParameterTypes = classMetadata.Parameters
            .Select(x => x.Type)
            .ToArray();
            
        var methodParameterTypes = methodMetadata.Parameters
            .Select(x => x.Type)
            .ToArray();

        var classTypeWithParameters = constructorParameterTypes.Length > 0
            ? $"{metadata.TestClassType.Name}({string.Join(", ", constructorParameterTypes.Select(t => t))})"
            : metadata.TestClassType.Name;

        var methodWithParameters = methodParameterTypes.Length > 0
            ? $"{metadata.TestMethodName}({string.Join(", ", methodParameterTypes.Select(t => t))})"
            : metadata.TestMethodName;

        return $"{methodMetadata.Class.Namespace}.{classTypeWithParameters}.{combination.ClassDataSourceIndex}.{combination.ClassLoopIndex}.{methodWithParameters}.{combination.MethodDataSourceIndex}.{combination.MethodLoopIndex}.{combination.RepeatIndex}";
    }

    public static string GenerateFailedTestId(TestMetadata metadata)
    {
        var methodMetadata = metadata.MethodMetadata;
        var classMetadata = methodMetadata.Class;
        
        var constructorParameterTypes = classMetadata.Parameters
            .Select(x => x.Type)
            .ToArray();
            
        var methodParameterTypes = methodMetadata.Parameters
            .Select(x => x.Type)
            .ToArray();

        var classTypeWithParameters = constructorParameterTypes.Length > 0
            ? $"{metadata.TestClassType.Name}({string.Join(", ", constructorParameterTypes.Select(t => t))})"
            : metadata.TestClassType.Name;

        var methodWithParameters = methodParameterTypes.Length > 0
            ? $"{metadata.TestMethodName}({string.Join(", ", methodParameterTypes.Select(t => t))})"
            : metadata.TestMethodName;

        return $"{methodMetadata.Class.Namespace}.{classTypeWithParameters}.0.0.{methodWithParameters}.0.0.0_DataGenerationError";
    }
}