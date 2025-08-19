namespace TUnit.TestProject.Bugs;

public abstract class GenericTestExample<T>
{
    [Test]
    public void GenericTest()
    {
        // This test runs only once, as expected.
        Console.WriteLine($"Running generic test with type: {typeof(T).Name}");
    }
}

[InheritsTests]
public class IntGenericTests : GenericTestExample<int>
{
    private static int _value = 0;
    
    [Test]
    public void AdditionalIntTest()
    {
        // This test is unexpectedly executed twice.
        _value++;
        Console.WriteLine($"IntGenericTests running with value: {_value}");
    }
}