using TUnit.Core;

namespace TUnit.TestProject;

public class TupleDataSourceDrivenTests
{
    [DataSourceDrivenTest]
    [MethodDataSource(nameof(TupleMethod), UnfoldTuple = true)]
    public void DataSource_TupleMethod(int value, string value2, bool value3)
    {
    }
    
    public static (int, string, bool) TupleMethod() => (1, "String", true);
}