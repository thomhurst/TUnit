using TUnit.Assertions;
using TUnit.Assertions.Extensions;
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
    
    [DataSourceDrivenTest(typeof(CommonTestData), nameof(CommonTestData.MultipleArgs))]
    public async Task Test2(int i, string i2, bool i3)
    {
        await Assert.That(i).Is.EqualTo(1);
        await Assert.That(i2).Is.EqualTo("2");
        await Assert.That(i3).Is.True();
    }
}