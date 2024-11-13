using FluentAssertions;

namespace TUnit.TestProject;

public class UniqueBuilderContextsOnEnumerableDataGeneratorTests
{
    [Test, UniqueBuilderContextsOnEnumerableDataGeneratorTestsGenerator]
    public void Test(int value)
    {
    }
}

public class UniqueBuilderContextsOnEnumerableDataGeneratorTestsGenerator : DataSourceGeneratorAttribute<int>
{
    public override IEnumerable<int> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var id1 = dataGeneratorMetadata.TestBuilderContext.Current.Id;
        var id2 = dataGeneratorMetadata.TestBuilderContext.Current.Id;
        
        yield return 1;
        
        var id3 = dataGeneratorMetadata.TestBuilderContext.Current.Id;
        
        yield return 2;
        
        var id4 = dataGeneratorMetadata.TestBuilderContext.Current.Id;

        id1.Should().Be(id2);
        
        id3.Should().NotBe(id1);
        
        id4.Should().NotBe(id1);
        id4.Should().NotBe(id3);
    }
}