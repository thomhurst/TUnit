using TUnit.Core;

namespace TUnit.TestProject;

[DataSourceDrivenTest]
[MethodData(typeof(CommonTestData), nameof(CommonTestData.One))]
[MethodData(typeof(CommonTestData), nameof(CommonTestData.Two))]
[MethodData(typeof(CommonTestData), nameof(CommonTestData.Three))]
public class DataSourceClassCombinedWithDataSourceMethod(int i)
{
    [DataSourceDrivenTest]
    [MethodData(typeof(CommonTestData), nameof(CommonTestData.One))]
    [MethodData(typeof(CommonTestData), nameof(CommonTestData.Two))]
    [MethodData(typeof(CommonTestData), nameof(CommonTestData.Three))]
    public void DataSourceClassCombinedWithDataSourceMethodTest(int i)
    {
    }
}