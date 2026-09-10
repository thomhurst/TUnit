using System.Threading;

namespace TUnit.TestProject.Bugs;

// Test case to reproduce issue #2952: Tests in derived class with [InheritsTests] attribute are executed twice
// Using non-abstract base class as suggested
public class Issue2952GenericBase<T>
{
    [Test]
    public void GenericBaseTest()
    {
        Console.WriteLine($"Issue2952: Running generic base test with type: {typeof(T).Name}");
    }
}

[InheritsTests]
public class Issue2952DerivedTests : Issue2952GenericBase<int>
{
    private static int _executionCount = 0;
    
    [Test]
    public void DerivedAdditionalTest()
    {
        var currentCount = Interlocked.Increment(ref _executionCount);
        Console.WriteLine($"Issue2952: DerivedAdditionalTest running - execution count: {currentCount}");
        
        // This test should only run once, so count should only be 1
        // If it runs twice, count will be 2 and we can detect the bug
        if (currentCount > 1)
        {
            throw new InvalidOperationException($"Issue2952: Test executed {currentCount} times - should only execute once!");
        }
    }
}