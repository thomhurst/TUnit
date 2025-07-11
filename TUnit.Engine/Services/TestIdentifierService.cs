using System.Text;
using TUnit.Core;
using TUnit.Core.Data;

namespace TUnit.Engine.Services;

internal static class TestIdentifierService
{
    public static string GenerateTestId(TestMetadata metadata, TestDataCombination combination)
    {
        var methodMetadata = metadata.MethodMetadata;
        var classMetadata = methodMetadata.Class;
        
        // Pre-size arrays to avoid LINQ chains and multiple enumerations
        var constructorParameters = classMetadata.Parameters;
        var constructorParameterTypes = new Type[constructorParameters.Length];
        for (int i = 0; i < constructorParameters.Length; i++)
        {
            constructorParameterTypes[i] = constructorParameters[i].Type;
        }
        
        var methodParameters = methodMetadata.Parameters;
        var methodParameterTypes = new Type[methodParameters.Length];
        for (int i = 0; i < methodParameters.Length; i++)
        {
            methodParameterTypes[i] = methodParameters[i].Type;
        }

        var classTypeWithParameters = BuildTypeWithParameters(metadata.TestClassType.Name, constructorParameterTypes);
        var methodWithParameters = BuildTypeWithParameters(metadata.TestMethodName, methodParameterTypes);

        // Use StringBuilder for efficient string concatenation
        var sb = new StringBuilder(256); // Pre-size for typical test ID length
        sb.Append(methodMetadata.Class.Namespace)
          .Append('.')
          .Append(classTypeWithParameters)
          .Append('.')
          .Append(combination.ClassDataSourceIndex)
          .Append('.')
          .Append(combination.ClassLoopIndex)
          .Append('.')
          .Append(methodWithParameters)
          .Append('.')
          .Append(combination.MethodDataSourceIndex)
          .Append('.')
          .Append(combination.MethodLoopIndex)
          .Append('.')
          .Append(combination.RepeatIndex);
          
        return sb.ToString();
    }

    public static string GenerateFailedTestId(TestMetadata metadata)
    {
        var methodMetadata = metadata.MethodMetadata;
        var classMetadata = methodMetadata.Class;
        
        // Pre-size arrays to avoid LINQ chains and multiple enumerations
        var constructorParameters = classMetadata.Parameters;
        var constructorParameterTypes = new Type[constructorParameters.Length];
        for (int i = 0; i < constructorParameters.Length; i++)
        {
            constructorParameterTypes[i] = constructorParameters[i].Type;
        }
        
        var methodParameters = methodMetadata.Parameters;
        var methodParameterTypes = new Type[methodParameters.Length];
        for (int i = 0; i < methodParameters.Length; i++)
        {
            methodParameterTypes[i] = methodParameters[i].Type;
        }

        var classTypeWithParameters = BuildTypeWithParameters(metadata.TestClassType.Name, constructorParameterTypes);
        var methodWithParameters = BuildTypeWithParameters(metadata.TestMethodName, methodParameterTypes);

        // Use StringBuilder for efficient string concatenation
        var sb = new StringBuilder(256); // Pre-size for typical test ID length
        sb.Append(methodMetadata.Class.Namespace)
          .Append('.')
          .Append(classTypeWithParameters)
          .Append(".0.0.")
          .Append(methodWithParameters)
          .Append(".0.0.0_DataGenerationError");
          
        return sb.ToString();
    }
    
    private static string BuildTypeWithParameters(string typeName, Type[] parameterTypes)
    {
        if (parameterTypes.Length == 0)
        {
            return typeName;
        }
        
        // Use StringBuilder for efficient parameter list construction
        var sb = new StringBuilder(typeName.Length + (parameterTypes.Length * 20)); // Estimate capacity
        sb.Append(typeName).Append('(');
        
        for (int i = 0; i < parameterTypes.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }
            sb.Append(parameterTypes[i]);
        }
        
        sb.Append(')');
        return sb.ToString();
    }
}