namespace TUnit.TestProject.Attributes;

public class EnumGeneratorAttribute : NonTypedDataSourceGeneratorAttribute
{
    public override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        if (dataGeneratorMetadata.ParameterInfos is not { Length: 1 })
        {
            throw new Exception("Expecting one parameter");
        }

        var parameterType = dataGeneratorMetadata.ParameterInfos[0].ParameterType;
        if (parameterType is not { IsEnum: true })
        {
            throw new Exception("Expecting Enum parameter");
        }
        
        foreach (var enumValue in Enum.GetValuesAsUnderlyingType(parameterType))
        {
            yield return () => [enumValue];
        }
    }
}