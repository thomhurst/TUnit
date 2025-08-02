namespace TUnit.TestProject;

public abstract class GenericTestExample<T>
{
    [Test]
    public void GenericTest()
    {
        // This is a test in a generic class
        Console.WriteLine($"Running test with type: {typeof(T).Name}");
    }

    [Test]
    [Arguments(5)]
    [Arguments("hello")]
    public void GenericTestWithArguments<TArg>(TArg value)
    {
        // This is a generic method with arguments
        Console.WriteLine($"Running test with value: {value} of type: {typeof(TArg).Name}");
    }
}

// Test instantiation with different types
[InheritsTests]
public class IntGenericTests : GenericTestExample<int> { }

[InheritsTests]
public class StringGenericTests : GenericTestExample<string> { }
