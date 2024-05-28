using TUnit.Core;

namespace TUnit.TestProject;

public class TupleDataSourceDrivenTests
{
    [DataSourceDrivenTest]
    [MethodDataSource(nameof(TupleMethod), UnfoldTuple = true)]
    public void DataSource_TupleMethod(int value, string value2, bool value3)
    {
        var tuple = TupleMethod();
        var (t, t2, t3) = tuple;
    }
    
    public static (int, string, bool) TupleMethod() => (1, "String", true);
}