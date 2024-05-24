using TUnit.Core;

namespace TUnit.TestProject;

public class DataSourceDrivenTests
{
    [DataSourceDrivenTest]
    [MethodDataSource(nameof(SomeMethod))]
    public void DataSource_Method(int value)
    {
    }
    
    [DataSourceDrivenTest]
    [ClassDataSource(typeof(SomeClass))]
    public void DataSource_Class(SomeClass value)
    {
    }
    
    [DataSourceDrivenTest]
    [MethodDataSource(nameof(TupleMethod), UnfoldTuple = true)]
    public void DataSource_TupleMethod(int value, string value2, bool value3)
    {
    }

    public static int SomeMethod() => 1;

    public record SomeClass
    {
        public int Value => 1;
    }

    public static (int, string, bool) TupleMethod() => (1, "String", true);
}