namespace TUnit.TestProject;

// Test using MethodDataSource with tuples for the class constructor
[MethodDataSource(nameof(GetClassTypedTuples))]
public class TupleUnwrappingTests(int number, string text)
{
    [Test]
    public async Task Test_ClassDataSource_With_Typed_Tuples()
    {
        await Assert.That(number).IsGreaterThan(0);
        await Assert.That(text).IsNotNull();
        
        // Log to verify the values are correctly unpacked
        Console.WriteLine($"Test executed with: number={number}, text={text}");
    }
    
    [Test]
    [MethodDataSource(nameof(GetMethodTypedTuples))]
    public async Task Test_MethodDataSource_With_Typed_Tuples(int value, string name)
    {
        await Assert.That(value).IsGreaterThan(0);
        await Assert.That(name).IsNotNull();
        
        Console.WriteLine($"Method test executed with: value={value}, name={name}");
    }
    
    public static (int, string)[] GetClassTypedTuples()
    {
        return [(1, "First"), (2, "Second"), (3, "Third")];
    }
    
    public static (int, string)[] GetMethodTypedTuples()
    {
        return [(10, "Ten"), (20, "Twenty"), (30, "Thirty")];
    }
}