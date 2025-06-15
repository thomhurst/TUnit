namespace TUnit.TestProject.Attributes;

public class EnumGeneratorAttribute : UntypedDataSourceGeneratorAttribute
{
    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        if (dataGeneratorMetadata.MembersToGenerate is not [SourceGeneratedParameterInformation sourceGeneratedParameterInformation])
        {
            throw new Exception("Expecting one parameter");
        }

        var parameterType = sourceGeneratedParameterInformation.Type;
        if (parameterType is not { IsEnum: true })
        {
            throw new Exception("Expecting Enum parameter");
        }

#if NET
        foreach (var enumValue in Enum.GetValuesAsUnderlyingType(parameterType))
        {
            yield return () => [enumValue];
        }
#else
        foreach (var enumValue in Enum.GetValues(parameterType))
        {
            yield return () => [enumValue];
        }
#endif

    }
}
