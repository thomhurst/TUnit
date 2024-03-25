using TUnit.Core;

namespace TUnit.TestProject;

[MethodData(typeof(CommonTestData), nameof(CommonTestData.One))]
[MethodData(typeof(CommonTestData), nameof(CommonTestData.Two))]
[MethodData(typeof(CommonTestData), nameof(CommonTestData.Three))]
public class DataSourceClassCombinedWithDataSourceMethod(int i)
{
    [MethodData(typeof(CommonTestData), nameof(CommonTestData.One))]
    [MethodData(typeof(CommonTestData), nameof(CommonTestData.Two))]
    [MethodData(typeof(CommonTestData), nameof(CommonTestData.Three))]
    public void Test(int i)
    {
    }
}