namespace TUnit.Data;

public class DataGenerator : DataSourceGeneratorAttribute<int, int, int>
{
    protected override IEnumerable<Func<(int, int, int)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => (1, 1, 2);
        yield return () => (1, 2, 3);
        yield return () => (4, 5, 9);
    }
}
