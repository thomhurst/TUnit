using System.Buffers;
using System.Text;
using TUnit.Core;
using TUnit.Engine.Building;

namespace TUnit.Engine.Services;

internal static class TestIdentifierService
{
    private const int MaxStackAllocSize = 16;

    public static string GenerateTestId(TestMetadata metadata, TestBuilder.TestData combination)
    {
        var methodMetadata = metadata.MethodMetadata;
        var classMetadata = methodMetadata.Class;

        var constructorParameters = classMetadata.Parameters;
        var methodParameters = methodMetadata.Parameters;

        // Use ArrayPool to avoid heap allocations for Type arrays
        // Note: Cannot use stackalloc because Type is a managed reference type
        var constructorParameterTypes = ArrayPool<Type>.Shared.Rent(constructorParameters.Length);
        var methodParameterTypes = ArrayPool<Type>.Shared.Rent(methodParameters.Length);

        try
        {
            // Fill arrays with actual types
            for (var i = 0; i < constructorParameters.Length; i++)
            {
                constructorParameterTypes[i] = constructorParameters[i].Type;
            }

            for (var i = 0; i < methodParameters.Length; i++)
            {
                methodParameterTypes[i] = methodParameters[i].Type;
            }

            var classTypeWithParameters = BuildTypeWithParameters(
                GetTypeNameWithGenerics(metadata.TestClassType),
                constructorParameterTypes.AsSpan(0, constructorParameters.Length));

            var methodWithParameters = BuildTypeWithParameters(
                metadata.TestMethodName,
                methodParameterTypes.AsSpan(0, methodParameters.Length));

            // Use StringBuilder for efficient string concatenation
            var sb = new StringBuilder(256); // Pre-size for typical test ID length
            sb.Append(methodMetadata.Class.Namespace)
              .Append('.')
              .Append(classTypeWithParameters)
              .Append('.')
              .Append(combination.ClassDataSourceAttributeIndex)
              .Append('.')
              .Append(combination.ClassDataLoopIndex)
              .Append('.')
              .Append(methodWithParameters)
              .Append('.')
              .Append(combination.MethodDataSourceAttributeIndex)
              .Append('.')
              .Append(combination.MethodDataLoopIndex)
              .Append('.')
              .Append(combination.RepeatIndex);

            // Add inheritance information to ensure uniqueness
            if (combination.InheritanceDepth > 0)
            {
                sb.Append("_inherited").Append(combination.InheritanceDepth);
            }

            return sb.ToString();
        }
        finally
        {
            ArrayPool<Type>.Shared.Return(constructorParameterTypes);
            ArrayPool<Type>.Shared.Return(methodParameterTypes);
        }
    }

    public static string GenerateFailedTestId(TestMetadata metadata)
    {
        // For backward compatibility, use default combination values
        return GenerateFailedTestId(metadata, new TestDataCombination());
    }

    public static string GenerateFailedTestId(TestMetadata metadata, TestDataCombination combination)
    {
        var methodMetadata = metadata.MethodMetadata;
        var classMetadata = methodMetadata.Class;

        var constructorParameters = classMetadata.Parameters;
        var methodParameters = methodMetadata.Parameters;

        // Use ArrayPool to avoid heap allocations for Type arrays
        var constructorParameterTypes = ArrayPool<Type>.Shared.Rent(constructorParameters.Length);
        var methodParameterTypes = ArrayPool<Type>.Shared.Rent(methodParameters.Length);

        try
        {
            // Fill arrays with actual types
            for (var i = 0; i < constructorParameters.Length; i++)
            {
                constructorParameterTypes[i] = constructorParameters[i].Type;
            }

            for (var i = 0; i < methodParameters.Length; i++)
            {
                methodParameterTypes[i] = methodParameters[i].Type;
            }

            var classTypeWithParameters = BuildTypeWithParameters(
                GetTypeNameWithGenerics(metadata.TestClassType),
                constructorParameterTypes.AsSpan(0, constructorParameters.Length));

            var methodWithParameters = BuildTypeWithParameters(
                metadata.TestMethodName,
                methodParameterTypes.AsSpan(0, methodParameters.Length));

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
              .Append(combination.RepeatIndex)
              .Append("_DataGenerationError");

            return sb.ToString();
        }
        finally
        {
            ArrayPool<Type>.Shared.Return(constructorParameterTypes);
            ArrayPool<Type>.Shared.Return(methodParameterTypes);
        }
    }

    private static string GetTypeNameWithGenerics(Type type)
    {
        var sb = new StringBuilder();

        // Build the full type hierarchy including all containing types
        var typeHierarchy = new List<string>();
        var currentType = type;

        while (currentType != null)
        {
            if (currentType.IsGenericType)
            {
                var typeSb = new StringBuilder();
                var name = currentType.Name;

                var backtickIndex = name.IndexOf('`');
                if (backtickIndex > 0)
                {
#if NET6_0_OR_GREATER
                    typeSb.Append(name.AsSpan(0, backtickIndex));
#else
                    typeSb.Append(name.Substring(0, backtickIndex));
#endif
                }
                else
                {
                    typeSb.Append(name);
                }

                // Add the generic type arguments
                var genericArgs = currentType.GetGenericArguments();
                typeSb.Append('<');
                for (var i = 0; i < genericArgs.Length; i++)
                {
                    if (i > 0)
                    {
                        typeSb.Append(", ");
                    }
                    // Use the full name for generic arguments to ensure uniqueness
                    typeSb.Append(genericArgs[i].FullName ?? genericArgs[i].Name);
                }
                typeSb.Append('>');

                typeHierarchy.Add(typeSb.ToString());
            }
            else
            {
                typeHierarchy.Add(currentType.Name);
            }

            currentType = currentType.DeclaringType;
        }

        // Reverse to get outer-to-inner order
        typeHierarchy.Reverse();

        // Append all types with dot separator
        for (var i = 0; i < typeHierarchy.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('.');
            }
            sb.Append(typeHierarchy[i]);
        }

        return sb.ToString();
    }

    private static string BuildTypeWithParameters(string typeName, ReadOnlySpan<Type> parameterTypes)
    {
        if (parameterTypes.Length == 0)
        {
            return typeName;
        }

        // Use StringBuilder for efficient parameter list construction
        var sb = new StringBuilder(typeName.Length + parameterTypes.Length * 20); // Estimate capacity
        sb.Append(typeName).Append('(');

        for (var i = 0; i < parameterTypes.Length; i++)
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
