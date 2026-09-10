using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.TestProject;

public class TestIdDebugTest
{
    private static readonly List<string> TestIds = new();
    private static readonly object Lock = new();

    [Test]
    [Repeat(3)]
    public async Task DebugTestIdGeneration()
    {
        await Task.Yield();

        var context = TestContext.Current!;
        var testDetails = context.Metadata.TestDetails;

        // Use reflection to see if there's a RepeatIndex property we're missing
        var properties = testDetails.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        lock (Lock)
        {
            Console.WriteLine($"=== Test Execution ===");
            Console.WriteLine($"TestId: {testDetails.TestId}");
            Console.WriteLine($"TestName: {testDetails.TestName}");
            Console.WriteLine($"HashCode: {context.GetHashCode()}");

            // Check all properties
            foreach (var prop in properties)
            {
                if (prop.Name.Contains("Repeat", StringComparison.OrdinalIgnoreCase) ||
                    prop.Name.Contains("Index", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var value = prop.GetValue(testDetails);
                        Console.WriteLine($"  {prop.Name}: {value}");
                    }
                    catch
                    {
                        // Ignore
                    }
                }
            }

            TestIds.Add(testDetails.TestId);
            Console.WriteLine($"Total unique TestIds so far: {new HashSet<string>(TestIds).Count}");
            Console.WriteLine("---");
        }
    }
}
