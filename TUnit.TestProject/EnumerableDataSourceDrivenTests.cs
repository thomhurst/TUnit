using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

public class EnumerableDataSourceDrivenTests
{
    [DataSourceDrivenTest]
    [EnumerableMethodData(nameof(SomeMethod))]
    public async Task DataSource_Method(int value)
    {
        await Assert.That(value).Is.EqualTo(1);
    }
    
    public static IEnumerable<int> SomeMethod() => [1,2,3,4,5];
}