using System;
using System.Linq;
using TUnit.Core.Helpers;

public class TestTupleUnwrapping
{
    public static void Main()
    {
        Console.WriteLine("Testing tuple unwrapping with DataSourceHelpers...");
        
        // Test 1: Simple tuple
        var tuple1 = (42, "Hello");
        var result1 = DataSourceHelpers.ProcessTestDataSource(tuple1);
        Console.WriteLine($"Tuple (42, \"Hello\") -> {result1.Length} factories");
        
        // Test 2: Nested tuple in array (simulating ClassDataSource return)
        var dataArray = new object?[] { (99, "World") };
        var firstElement = dataArray[0];
        var unwrapped = DataSourceHelpers.UnwrapTupleAot(firstElement);
        Console.WriteLine($"Array with tuple -> {unwrapped.Length} elements:");
        for (int i = 0; i < unwrapped.Length; i++)
        {
            Console.WriteLine($"  [{i}] = {unwrapped[i]}");
        }
        
        // Test 3: ProcessTestDataSource with the tuple directly
        var factories = DataSourceHelpers.ProcessTestDataSource((123, "Test"));
        Console.WriteLine($"\nProcessTestDataSource on tuple -> {factories.Length} factories");
        
        Console.WriteLine("\nAll tests completed!");
    }
}