using TUnit.Core;

namespace TUnit.TestProject;

[DataSourceDrivenTest]
[MethodDataSource(typeof(CommonTestData), nameof(CommonTestData.One))]
[MethodDataSource(typeof(CommonTestData), nameof(CommonTestData.Two))]
[MethodDataSource(typeof(CommonTestData), nameof(CommonTestData.Three))]
public class DataSourceClassCombinedWithDataSourceMethod(int i)
{
    [DataSourceDrivenTest]
    [MethodDataSource(typeof(CommonTestData), nameof(CommonTestData.One))]
    [MethodDataSource(typeof(CommonTestData), nameof(CommonTestData.Two))]
    [MethodDataSource(typeof(CommonTestData), nameof(CommonTestData.Three))]
    public void DataSourceClassCombinedWithDataSourceMethodTest(int i)
    {
    }
}