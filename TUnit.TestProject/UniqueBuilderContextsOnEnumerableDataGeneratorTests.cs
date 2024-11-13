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
        Console.WriteLine(dataGeneratorMetadata.TestBuilderContext.Current.Id);
        Console.WriteLine(dataGeneratorMetadata.TestBuilderContext.Current.Id);

        yield return 1;
        
        Console.WriteLine(dataGeneratorMetadata.TestBuilderContext.Current.Id);
        
        yield return 2;
        
        Console.WriteLine(dataGeneratorMetadata.TestBuilderContext.Current.Id);
    }
}