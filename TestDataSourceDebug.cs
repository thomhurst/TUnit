using System;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.Core.Helpers;

public class TestDataSourceDebug
{
    public static async Task Main()
    {
        Console.WriteLine("=== Testing ClassDataSource with TupleDataSource ===");
        
        // Create a TupleDataSource instance
        var dataSource = new TUnit.TestProject.TupleDataSource();
        
        // Create metadata for the data generator
        var metadata = new DataGeneratorMetadata
        {
            Type = DataGeneratorType.ClassParameters,
            TestBuilderContext = new TestBuilderContext(),
            MembersToGenerate = new MemberMetadata[]
            {
                new ParameterMetadata(typeof(int)) { Name = "number" },
                new ParameterMetadata(typeof(string)) { Name = "text" }
            },
            TestInformation = new MethodMetadata
            {
                Type = typeof(TestDataSourceDebug),
                Name = "Test",
                GenericTypeCount = 0,
                ReturnType = typeof(Task),
                Parameters = Array.Empty<ParameterMetadata>()
            },
            TestSessionId = "test-session",
            TestClassInstance = null,
            ClassInstanceArguments = null
        };
        
        Console.WriteLine("\nIterating through data source...");
        
        await foreach (var dataSourceFunc in dataSource.GenerateAsync(metadata))
        {
            var data = await dataSourceFunc();
            Console.WriteLine($"\nReceived data array with {data?.Length ?? 0} elements:");
            
            if (data != null && data.Length > 0)
            {
                var firstElement = data[0];
                Console.WriteLine($"  First element type: {firstElement?.GetType()}");
                Console.WriteLine($"  First element value: {firstElement}");
                
                // Try unwrapping
                var unwrapped = DataSourceHelpers.UnwrapTupleAot(firstElement);
                Console.WriteLine($"  Unwrapped to {unwrapped.Length} elements:");
                for (int i = 0; i < unwrapped.Length; i++)
                {
                    Console.WriteLine($"    [{i}] = {unwrapped[i]} (type: {unwrapped[i]?.GetType()})");
                }
            }
        }
        
        Console.WriteLine("\n=== Test Complete ===");
    }
}

// Simple test context implementation
public class TestBuilderContext
{
    // Empty implementation for testing
}