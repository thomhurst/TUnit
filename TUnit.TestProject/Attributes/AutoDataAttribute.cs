using AutoFixture.Kernel;

namespace TUnit.TestProject.Attributes;

public class AutoDataAttribute : UntypedDataSourceGeneratorAttribute
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
            var type = member switch
            {
                PropertyMetadata prop => prop.Type,
                ParameterMetadata param => param.Type,
                ClassMetadata cls => cls.Type,
                MethodMetadata method => method.Type,
                _ => throw new InvalidOperationException($"Unknown member type: {member.GetType()}")
            };
            yield return Fixture.Create(type, new SpecimenContext(Fixture));
        }
    }
}
