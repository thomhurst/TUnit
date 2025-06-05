
using TUnit.TestProject.Attributes;

#pragma warning disable CS9113 // Parameter is unread.
namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[AutoFixtureGenerator<int, string, bool>]
[AutoFixtureGenerator]
public class DataSourceGeneratorTests(int value, string value2, bool value3)
{
    [Test]
    [AutoFixtureGenerator<int>]
    public void GeneratedData_Method(int value)
    {
        var data = value;
        var attrs = TestContext.Current!.TestDetails.Attributes;
    }

    [Test]
    [AutoFixtureGenerator<int, string, bool>]
    public void GeneratedData_Method2(int value, string value2, bool value3)
    {
        // Dummy method
    }

    [Test]
    [AutoFixtureGenerator]
    public void GeneratedData_Method3(int value, string value2, bool value3)
    {
        // Dummy method
    }

    public class AutoFixtureGeneratorAttribute<T> : DataSourceGeneratorAttribute<T>
    {
        public override IEnumerable<Func<T>> GenerateDataSources(DataGeneratorMetadata metadata)
        {
            return [() => default!];
        }
    }

    public class AutoFixtureGeneratorAttribute<T1, T2, T3> : DataSourceGeneratorAttribute<T1, T2, T3>
    {
        public override IEnumerable<Func<(T1, T2, T3)>> GenerateDataSources(DataGeneratorMetadata metadata)
        {
            return [() => default];
        }
    }

    public class AutoFixtureGeneratorAttribute : DataSourceGeneratorAttribute<int, string, bool>
    {
        public override IEnumerable<Func<(int, string, bool)>> GenerateDataSources(DataGeneratorMetadata metadata)
        {
            return [() => default];
        }
    }
}