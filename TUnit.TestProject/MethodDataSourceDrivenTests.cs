using TUnit.Core;

namespace TUnit.TestProject;

public class MethodDataSourceDrivenTests
{
    [DataSourceDrivenTest]
    [MethodDataSource(nameof(SomeMethod))]
    public void DataSource_Method(int value)
    {
        // Dummy method
    }
    
    [DataSourceDrivenTest]
    [MethodDataSource(nameof(SomeMethod), DisposeAfterTest = false)]
    public void DataSource_Method2(int value)
    {
        // Dummy method
    }

    public static int SomeMethod() => 1;
}