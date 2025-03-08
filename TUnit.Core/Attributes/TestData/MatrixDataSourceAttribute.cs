using TUnit.Core.Enums;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class MatrixDataSourceAttribute : NonTypedDataSourceGeneratorAttribute
{
    public override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var parameterInformation = dataGeneratorMetadata
            .MembersToGenerate
            .OfType<SourceGeneratedParameterInformation>()
            .ToArray();

        if (parameterInformation.Length != dataGeneratorMetadata.MembersToGenerate.Length
            || parameterInformation.Length is 0)
        {
            throw new Exception("[MatrixDataSource] only supports parameterised tests");
        }
        
        var exclusions = GetExclusions(dataGeneratorMetadata.Type == DataGeneratorType.TestParameters
        ? dataGeneratorMetadata.TestInformation.Attributes : dataGeneratorMetadata.TestInformation.Class.Attributes);
        
        foreach (var row in GetMatrixValues(parameterInformation.Select(GetAllArguments)))
        {
            if (exclusions.Any(e => e.SequenceEqual(row)))
            {
                continue;
            }
            
            yield return () => row.ToArray();
        }
    }

    private object?[][] GetExclusions(Attribute[] attributes)
    {
        return attributes.OfType<MatrixExclusionAttribute>()
            .Select(x => x.Objects)
            .ToArray();
    }

    private IReadOnlyList<object?> GetAllArguments(SourceGeneratedParameterInformation sourceGeneratedParameterInformation)
    {
        var matrixAttribute = sourceGeneratedParameterInformation.Attributes.OfType<MatrixAttribute>().FirstOrDefault();

        var type = sourceGeneratedParameterInformation.Type;

        if (matrixAttribute is null or { Objects.Length: 0 })
        {
            if (type == typeof(bool))
            {
                return [true, false];
            }

            if (!type.IsEnum)
            {
                throw new ArgumentNullException(
                    $"No MatrixAttribute found for parameter {sourceGeneratedParameterInformation.Name}");
            }
        }

        if (type.IsEnum && matrixAttribute?.Objects is null or { Length: 0 })
        {
#if NET
            return Enum.GetValuesAsUnderlyingType(type)
                .Cast<object>()
                .Except(matrixAttribute?.Excluding?.Select(e => Convert.ChangeType(e, Enum.GetUnderlyingType(type))) ?? [])
                .ToArray();
#else
            return Enum.GetValues(type)
                .Cast<object>()
                .Except(matrixAttribute?.Excluding?.Select(e => Convert.ChangeType(e, Enum.GetUnderlyingType(type))) ?? [])
                .ToArray();
#endif
        }

        if (matrixAttribute?.Excluding is not null)
        {
            return matrixAttribute.Objects.Except(matrixAttribute.Excluding).ToArray();
        }
        
        return matrixAttribute!.Objects;
    }
    
    private readonly IEnumerable<IEnumerable<object?>> _seed = [[]];

    private IEnumerable<IEnumerable<object?>> GetMatrixValues(IEnumerable<IReadOnlyList<object?>> elements)
    {
        return elements.Aggregate(_seed, (accumulator, enumerable)
            => accumulator.SelectMany(x => enumerable.Select(x.Append)));
    }
}