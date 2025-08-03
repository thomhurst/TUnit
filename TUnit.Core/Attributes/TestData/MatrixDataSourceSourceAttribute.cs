using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Enums;
using TUnit.Core.Extensions;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
[UnconditionalSuppressMessage("AOT", "IL2109:Type 'MatrixDataSourceAttribute' derives from base class with RequiresUnreferencedCodeAttribute", 
    Justification = "Matrix data source implementation is AOT-compatible with proper enum field preservation")]
public sealed class MatrixDataSourceAttribute : UntypedDataSourceGeneratorAttribute, IAccessesInstanceData
{
    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var parameterInformation = dataGeneratorMetadata
            .MembersToGenerate
            .OfType<ParameterMetadata>()
            .ToArray();

        if (parameterInformation.Length != dataGeneratorMetadata.MembersToGenerate.Length
            || parameterInformation.Length is 0)
        {
            throw new Exception("[MatrixDataSource] only supports parameterised tests");
        }

        if (dataGeneratorMetadata.TestInformation == null)
        {
            throw new InvalidOperationException("MatrixDataSource requires test information but none is available. This may occur during static property initialization.");
        }

        var testInformation = dataGeneratorMetadata.TestInformation;

        var classType = testInformation.Class.Type;


        var exclusions = GetExclusions(dataGeneratorMetadata.Type == DataGeneratorType.TestParameters
            ? dataGeneratorMetadata.TestInformation.GetCustomAttributes()
            : classType.GetCustomAttributesSafe());

        foreach (var row in GetMatrixValues(parameterInformation.Select(p => GetAllArguments(dataGeneratorMetadata, p))))
        {
            if (exclusions.Any(e => e.SequenceEqual(row)))
            {
                continue;
            }

            yield return () => row.ToArray();
        }
    }

    private object?[][] GetExclusions(IEnumerable<Attribute> attributes)
    {
        return attributes
            .OfType<MatrixExclusionAttribute>()
            .Select(x => x.Objects)
            .ToArray();
    }

    [UnconditionalSuppressMessage("AOT", "IL2072:Target parameter argument does not satisfy DynamicallyAccessedMemberTypes requirements", 
        Justification = "Test parameter types are comprehensively preserved by the source generation system for matrix data scenarios")]
    private IReadOnlyList<object?> GetAllArguments(DataGeneratorMetadata dataGeneratorMetadata,
        ParameterMetadata sourceGeneratedParameterInformation)
    {
        if (sourceGeneratedParameterInformation.ReflectionInfo == null)
        {
            throw new InvalidOperationException($"Parameter reflection information is not available for parameter '{sourceGeneratedParameterInformation.Name}'. This typically occurs when using instance method data sources which are not supported at compile time.");
        }

        var matrixAttribute = sourceGeneratedParameterInformation.ReflectionInfo.GetCustomAttributesSafe()
            .OfType<MatrixAttribute>()
            .FirstOrDefault();

        // Check if this is an instance data attribute and we don't have an instance
        if (matrixAttribute is IAccessesInstanceData && dataGeneratorMetadata.TestClassInstance == null)
        {
            var className = dataGeneratorMetadata.TestInformation?.Class.Type.Name ?? "Unknown";
            if (dataGeneratorMetadata.TestInformation?.Class.Type.IsGenericTypeDefinition ?? false)
            {
                throw new InvalidOperationException(
                    $"Cannot use MatrixInstanceMethod attribute in generic class '{className}' when the generic type parameters " +
                    $"must be inferred from the matrix values. This creates a circular dependency: " +
                    $"the instance is needed to get the matrix values, but the generic types (which come from the matrix values) " +
                    $"are needed to create the instance. Consider using static methods for matrix data sources in generic classes, " +
                    $"or provide the generic type arguments explicitly using [Arguments] or other data source attributes.");
            }

            throw new InvalidOperationException(
                $"Instance is required for MatrixInstanceMethod but no instance is available. " +
                $"This typically happens when the test class requires data that hasn't been expanded yet.");
        }

        var objects = matrixAttribute?.GetObjects(dataGeneratorMetadata);

        if (matrixAttribute is not null && objects is { Length: > 0 })
        {
            return matrixAttribute.Excluding is not null
                       ? objects.Except(matrixAttribute.Excluding).ToArray()
                       : objects;
        }

        var type = sourceGeneratedParameterInformation.Type;
        
        // Use the IsNullable property for AOT-safe nullable detection
        Type? underlyingType = null;
        var isNullable = sourceGeneratedParameterInformation.IsNullable;
        
        if (isNullable)
        {
            // Try to get underlying type, but if it fails in AOT, we'll handle it
            underlyingType = Nullable.GetUnderlyingType(type);
            
            // If Nullable.GetUnderlyingType failed but we know it's nullable from metadata,
            // check if it's a generic type with one type argument
            if (underlyingType == null && type.IsGenericType && type.GetGenericArguments().Length == 1)
            {
                underlyingType = type.GetGenericArguments()[0];
            }
        }
        
        var resolvedType = underlyingType ?? type;
        if (resolvedType != typeof(bool) && !resolvedType.IsEnum)
        {
            throw new ArgumentNullException($"No MatrixAttribute found for parameter {sourceGeneratedParameterInformation.Name}");
        }

        if (resolvedType == typeof(bool))
        {
            if (matrixAttribute?.Excluding is not null)
            {
                throw new InvalidOperationException("Do not exclude values from a boolean.");
            }

            return isNullable ? [true, false, null] : [true, false];
        }

#if NET
        var enumValues = Enum.GetValuesAsUnderlyingType(resolvedType)
                             .Cast<object?>();
#else
        var enumValues = Enum.GetValues(resolvedType)
                             .Cast<object?>();
#endif
        if (isNullable)
        {
            enumValues = enumValues.Append(null);
            if (matrixAttribute?.Excluding?.Any(x => x is null) ?? false)
            {
                throw new InvalidOperationException("Do not exclude null from a nullable enum - instead use the enum directly");
            }
        }

        return enumValues
#if NET
               .Except(matrixAttribute?.Excluding?.Select(e => Convert.ChangeType(e, Enum.GetUnderlyingType(resolvedType))) ?? [])
#else
               .Except(matrixAttribute?.Excluding ?? [])
#endif
               .ToArray();
    }

    private readonly IEnumerable<IEnumerable<object?>> _seed = [[]];

    private IEnumerable<IEnumerable<object?>> GetMatrixValues(IEnumerable<IReadOnlyList<object?>> elements)
    {
        return elements.Aggregate(_seed, (accumulator, enumerable)
            => accumulator.SelectMany(x => enumerable.Select(x.Append)));
    }

}
