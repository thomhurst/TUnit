using Shouldly;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class UniqueBuilderContextsOnEnumerableDataGeneratorTests
{
    [Test, UniqueBuilderContextsOnEnumerableDataGeneratorTestsGenerator]
    public void Test(int value)
    {
    }
}

public class UniqueBuilderContextsOnEnumerableDataGeneratorTestsGenerator : DataSourceGeneratorAttribute<int>
{
    protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var id1 = dataGeneratorMetadata.TestBuilderContext.Current.Id;
        var id2 = dataGeneratorMetadata.TestBuilderContext.Current.Id;

        yield return () => 1;

        var id3 = dataGeneratorMetadata.TestBuilderContext.Current.Id;

        yield return () => 2;

        var id4 = dataGeneratorMetadata.TestBuilderContext.Current.Id;

        id1.ShouldBe(id2);

        id3.ShouldNotBe(id1);

        id4.ShouldNotBe(id1);
        id4.ShouldNotBe(id3);
    }
}
