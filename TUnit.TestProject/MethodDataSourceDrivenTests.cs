// ReSharper disable UseCollectionExpression

using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
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
    [MethodDataSource(nameof(SomeMethod))]
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
    [MethodDataSource(nameof(SomeMethod), Arguments = [5])]
    [MethodDataSource(nameof(SomeMethod), Arguments = new object[] { 5 })]
    public void DataSource_Method3(int value)
    {
        Console.WriteLine(value);
        // Dummy method
    }

    [Test]
    [MethodDataSource(nameof(SomeMethod), Arguments = ["Hello World!", 5, true])]
    [MethodDataSource(nameof(SomeMethod), Arguments = new object[] { "Hello World!", 6, true })]
    [MethodDataSource(nameof(SomeMethod), Arguments = [MyString, 7, true])]
    [MethodDataSource(nameof(SomeMethod), Arguments = new object[] { MyString, 8, true })]
    public void DataSource_Method4(int value)
    {
        Console.WriteLine(value);
        // Dummy method
    }

    [Test]
    [MethodDataSource(nameof(MethodWithBaseReturn))]
    public void DataSource_WithBaseReturn(BaseValue value)
    {
    }

    [Test]
    [MethodDataSource(nameof(EnumerableFuncArrayTestData))]
    public async Task EnumerableFuncArrayTest(string[] strings)
    {
        await Assert.That(strings).IsTypeOf<string[]>();
    }

    public static IEnumerable<Func<string[]>> EnumerableFuncArrayTestData()
    {
        return
        [
            () => ["str1", "str2"],
            () => ["str3", "str4"]
        ];
    }

    public static int SomeMethod() => 1;

    public static Func<Action> SomeAction() => () => () => { };

    public static int SomeMethod(int input) => input * 2;
    public static int SomeMethod(string input1, int input2, bool input3) => input2 * 2;

    public static Func<BaseValue> MethodWithBaseReturn() => () => new ConcreteValue();

    public abstract class BaseValue;

    public class ConcreteValue : BaseValue;
}