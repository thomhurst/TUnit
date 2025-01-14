using AutoFixture.Kernel;

namespace TUnit.TestProject.Attributes;

public class AutoDataAttribute : NonTypedDataSourceGeneratorAttribute
{
    private static AutoFixture.Fixture _fixture = new();
    
    public override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => GenerateRow(dataGeneratorMetadata);
    }

    private object?[] GenerateRow(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return GenerateRowEnumerable(dataGeneratorMetadata).ToArray();
    }

    private static IEnumerable<object> GenerateRowEnumerable(DataGeneratorMetadata dataGeneratorMetadata)
    {
        foreach (var parameterInfo in dataGeneratorMetadata.ParameterInfos ?? [])
        {
            yield return _fixture.Create(parameterInfo.ParameterType, new SpecimenContext(_fixture));
        }
    }
}