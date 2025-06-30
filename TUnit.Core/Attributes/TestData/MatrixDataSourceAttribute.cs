using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Enums;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
[RequiresDynamicCode("MatrixDataSourceAttribute requires dynamic code generation for runtime matrix generation and enum reflection. This attribute is inherently incompatible with AOT compilation.")]
[RequiresUnreferencedCode("MatrixDataSourceAttribute may require unreferenced code for enum reflection and matrix generation. This attribute is inherently incompatible with AOT compilation.")]
public sealed class MatrixDataSourceAttribute : UntypedDataSourceGeneratorAttribute
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

        var exclusions = GetExclusions(dataGeneratorMetadata.Type == DataGeneratorType.TestParameters
        ? dataGeneratorMetadata.TestInformation.Attributes : dataGeneratorMetadata.TestInformation.Class.Attributes);

        foreach (var row in GetMatrixValues(parameterInformation.Select(p => GetAllArguments(dataGeneratorMetadata, p))))
        {
            if (exclusions.Any(e => e.SequenceEqual(row)))
            {
                continue;
            }

            yield return () => row.ToArray();
        }
    }

    private object?[][] GetExclusions(AttributeMetadata[] attributes)
    {
        return attributes
            .Select(a => a.Instance)
            .OfType<MatrixExclusionAttribute>()
            .Select(x => x.Objects)
            .ToArray();
    }

    private IReadOnlyList<object?> GetAllArguments(DataGeneratorMetadata dataGeneratorMetadata,
        ParameterMetadata sourceGeneratedParameterInformation)
    {
        var matrixAttribute = sourceGeneratedParameterInformation.Attributes
            .Select(a => a.Instance)
            .OfType<MatrixAttribute>()
            .FirstOrDefault();

        var objects = matrixAttribute?.GetObjects(dataGeneratorMetadata.TestClassInstance);

        if (matrixAttribute is not null && objects is { Length: > 0 })
        {
            return matrixAttribute.Excluding is not null
                       ? objects.Except(matrixAttribute.Excluding).ToArray()
                       : objects;
        }

        var type = sourceGeneratedParameterInformation.Type;
        var underlyingType = Nullable.GetUnderlyingType(type);
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

            return underlyingType is null ? [true, false] : [true, false, null];
        }

#if NET
        var enumValues = Enum.GetValuesAsUnderlyingType(resolvedType)
                             .Cast<object?>();
#else
        var enumValues = Enum.GetValues(resolvedType)
                             .Cast<object?>();
#endif
        if (underlyingType is not null)
        {
            enumValues = enumValues.Append(null);
            if (matrixAttribute?.Excluding?.Any(x => x is null) ?? false)
            {
                throw new InvalidOperationException("Do not exclude null from a nullable enum - instead use the enum directly");
            }
        }

        return enumValues
#if NET
               .Except(matrixAttribute?.Excluding?.Select(e => Convert.ChangeType(e, Enum.GetUnderlyingType(type))) ?? [])
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
