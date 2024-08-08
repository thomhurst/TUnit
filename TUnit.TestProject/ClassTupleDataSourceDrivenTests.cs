namespace TUnit.TestProject;

[MethodDataSource(nameof(TupleMethod), UnfoldTuple = true)]
public class ClassTupleDataSourceDrivenTests
{
    public ClassTupleDataSourceDrivenTests(int value, string value2, bool value3)
    {
        // Dummy method
    }
    
    [Test]
    [MethodDataSource(nameof(TupleMethod), UnfoldTuple = true)]
    public void DataSource_TupleMethod(int value, string value2, bool value3)
    {
        // Dummy method
    }
    
    public static (int, string, bool) TupleMethod() => (1, "String", true);
}