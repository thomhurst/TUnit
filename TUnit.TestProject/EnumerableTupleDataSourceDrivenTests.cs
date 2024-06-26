using TUnit.Core;

namespace TUnit.TestProject;

public class EnumerableTupleDataSourceDrivenTests
{
    [DataSourceDrivenTest]
    [EnumerableMethodDataSource(nameof(TupleMethod), UnfoldTuple = true)]
    public void DataSource_TupleMethod(int value, string value2, bool value3)
    {
        // Dummy method
    }
    
    public static IEnumerable<(int, string, bool)> TupleMethod()
    {
        yield return (1, "String", true);
        yield return (2, "String2", false);
        yield return (3, "String3", true);
    }
}