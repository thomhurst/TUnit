// ReSharper disable UseCollectionExpression

namespace TUnit.TestProject;

public class MethodDataSourceDrivenTests
{
    public const string MyString = "Hello World!";
    
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
    
    [Test]
    [MethodDataSource(nameof(SomeAction))]
    public void DataSource_Method_WithAction(Action action)
    {
        // Dummy method
    }
    
    [Test]
    [MethodDataSource(nameof(SomeMethod), Arguments = [5], DisposeAfterTest = false)]
    [MethodDataSource(nameof(SomeMethod), Arguments = new object[] { 5 }, DisposeAfterTest = false)]
    public void DataSource_Method3(int value)
    {
        Console.WriteLine(value);
        // Dummy method
    }
    
    [Test]
    [MethodDataSource(nameof(SomeMethod), Arguments = ["Hello World!", 5, true], DisposeAfterTest = false)]
    [MethodDataSource(nameof(SomeMethod), Arguments = new object[] { "Hello World!", 5, true }, DisposeAfterTest = false)]
    [MethodDataSource(nameof(SomeMethod), Arguments = [MyString, 5, true], DisposeAfterTest = false)]
    [MethodDataSource(nameof(SomeMethod), Arguments = new object[] { MyString, 5, true }, DisposeAfterTest = false)]
    public void DataSource_Method4(int value)
    {
        Console.WriteLine(value);
        // Dummy method
    }

    public static int SomeMethod() => 1;

    public static Action SomeAction() => () => { };
    
    public static int SomeMethod(int input) => input * 2;
    public static int SomeMethod(string input1, int input2, bool input3) => input2 * 2;
}