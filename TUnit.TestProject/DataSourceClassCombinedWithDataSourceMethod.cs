using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[MethodDataSource(typeof(CommonTestData), nameof(CommonTestData.One))]
[MethodDataSource(typeof(CommonTestData), nameof(CommonTestData.Two))]
[MethodDataSource(typeof(CommonTestData), nameof(CommonTestData.Three))]
public class DataSourceClassCombinedWithDataSourceMethod(int i)
{
    private readonly int _i = i;

    [Test]
    [MethodDataSource(typeof(CommonTestData), nameof(CommonTestData.One))]
    [MethodDataSource(typeof(CommonTestData), nameof(CommonTestData.Two))]
    [MethodDataSource(typeof(CommonTestData), nameof(CommonTestData.Three))]
    public void DataSourceClassCombinedWithDataSourceMethodTest(int i)
    {
        Console.WriteLine(i);
    }
}
