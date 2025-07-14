using TUnit.Core;

namespace TUnit.TestProject;

// Data source class that returns tuples for constructor parameters
public class TupleDataSource : IAsyncDataSourceGeneratorAttribute
{
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GenerateAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Return tuples that should be unwrapped into constructor parameters
        yield return () => Task.FromResult<object?[]?>(new object?[] { (42, "Hello") });
        yield return () => Task.FromResult<object?[]?>(new object?[] { (99, "World") });
        yield return () => Task.FromResult<object?[]?>(new object?[] { (123, "Test") });
        await Task.CompletedTask; // To satisfy async
    }
}

// Test class that expects two constructor parameters from tuple unwrapping
#pragma warning disable TUnit0001 // Attribute argument types don't match - tuple unwrapping is handled at runtime
[ClassDataSource<TupleDataSource>]
public class ClassDataSourceTupleTests(int number, string text)
{
    [Test]
    public async Task Test_ClassDataSource_UnwrapsTuples()
    {
        await Assert.That(number).IsGreaterThan(0);
        await Assert.That(text).IsNotNull();
        
        // Log to verify the values are correctly unpacked
        Console.WriteLine($"Test executed with: number={number}, text={text}");
    }
}

// Another test using static method that returns a single tuple
public class StaticMethodTupleDataSource
{
    public static (int, string) GetData() => (555, "Static");
}

#pragma warning disable TUnit0001 // Attribute argument types don't match - tuple unwrapping is handled at runtime
[MethodDataSource(typeof(StaticMethodTupleDataSource), nameof(StaticMethodTupleDataSource.GetData))]
public class StaticMethodClassDataSourceTupleTests(int number, string text)
{
    [Test]
    public async Task Test_StaticMethod_ClassDataSource_UnwrapsTuples()
    {
        await Assert.That(number).IsEqualTo(555);
        await Assert.That(text).IsEqualTo("Static");
        
        Console.WriteLine($"Static method test executed with: number={number}, text={text}");
    }
}