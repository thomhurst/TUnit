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

    public static int SomeMethod() => 1;
}