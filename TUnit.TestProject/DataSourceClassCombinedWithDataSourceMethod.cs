using TUnit.Core;

namespace TUnit.TestProject;

[DataSourceDrivenTest(typeof(CommonTestData), nameof(CommonTestData.One))]
[DataSourceDrivenTest(typeof(CommonTestData), nameof(CommonTestData.Two))]
[DataSourceDrivenTest(typeof(CommonTestData), nameof(CommonTestData.Three))]
public class DataSourceClassCombinedWithDataSourceMethod(int i)
{
    [DataSourceDrivenTest(typeof(CommonTestData), nameof(CommonTestData.One))]
    [DataSourceDrivenTest(typeof(CommonTestData), nameof(CommonTestData.Two))]
    [DataSourceDrivenTest(typeof(CommonTestData), nameof(CommonTestData.Three))]
    public void Test(int i)
    {
    }
}