namespace TUnit.TestProject;

public class MethodDataSourceDrivenTests
{
    [Test]
    [MethodDataSource(nameof(SomeMethod))]
    public void DataSource_Method(int value)
    {
        // Dummy method
    }
    
    [Test]
    [MethodDataSource(nameof(SomeMethod), DisposeAfterTest = false)]
    public void DataSource_Method2(int value)
    {
        // Dummy method
    }

    public static int SomeMethod() => 1;

    [Test]
    [MethodDataSource(nameof(SomeAction))]
    public void DataSource_Method_WithAction(Action action)
    {
        // Dummy method
    }

    public static Action SomeAction() => () => { };
}