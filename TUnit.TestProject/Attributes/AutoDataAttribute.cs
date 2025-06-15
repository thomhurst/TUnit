using AutoFixture.Kernel;

namespace TUnit.TestProject.Attributes;

public class AutoDataAttribute : NonTypedDataSourceGeneratorAttribute
{
    private static readonly AutoFixture.Fixture Fixture = new();

    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => GenerateRow(dataGeneratorMetadata);
    }

    private object?[] GenerateRow(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return GenerateRowEnumerable(dataGeneratorMetadata).ToArray();
    }

    private static IEnumerable<object> GenerateRowEnumerable(DataGeneratorMetadata dataGeneratorMetadata)
    {
        foreach (var member in dataGeneratorMetadata.MembersToGenerate)
        {
            yield return Fixture.Create(member.Type, new SpecimenContext(Fixture));
        }
    }
}
