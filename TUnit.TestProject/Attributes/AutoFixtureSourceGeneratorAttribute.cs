namespace TUnit.TestProject.Attributes;

public class AutoFixtureGeneratorAttribute<T> : DataSourceGeneratorAttribute<T>
{
    protected override IEnumerable<Func<T>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        return [() => default(T)!];
    }
}

public class AutoFixtureGeneratorAttribute<T1, T2, T3> : DataSourceGeneratorAttribute<T1, T2, T3>
{
    protected override IEnumerable<Func<(T1, T2, T3)>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        return [() => default((T1, T2, T3))];
    }
}

public class AutoFixtureGeneratorAttribute : DataSourceGeneratorAttribute<int, string, bool>
{
    protected override IEnumerable<Func<(int, string, bool)>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        return [() => default((int, string, bool))];
    }
}
