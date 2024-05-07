using TUnit.Core;

namespace TUnit.TestProject;

public class EnumerableDataSourceDrivenTests
{
    [DataSourceDrivenTest]
    [EnumerableMethodData(nameof(SomeMethod))]
    public void DataSource_Method(int value)
    {
    }
    
    public static IEnumerable<int> SomeMethod() => [1,2,3,4,5];
}