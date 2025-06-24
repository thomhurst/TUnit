using System;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.Engine;

// Test program to verify new architecture
class Program
{
    static async Task Main()
    {
        Console.WriteLine("Testing new architecture...");
        
        // Create test metadata
        var metadata = new TestMetadata
        {
            TestId = "test1",
            TestName = "TestMethod",
            TestClassType = typeof(TestClass),
            TestMethodName = "TestMethod",
            InstanceFactory = () => new TestClass(),
            TestInvoker = async (instance, args) => 
            {
                await ((TestClass)instance).TestMethod();
            },
            ParameterCount = 0,
            ParameterTypes = Array.Empty<Type>()
        };
        
        // Create test factory
        var factory = new TestFactory();
        
        // Create executable test
        var executableTests = await factory.CreateExecutableTests(metadata);
        
        Console.WriteLine($"Created {executableTests.Count} executable test(s)");
        
        foreach (var test in executableTests)
        {
            Console.WriteLine($"Test ID: {test.TestId}");
            Console.WriteLine($"Display Name: {test.DisplayName}");
        }
    }
}

class TestClass
{
    public async Task TestMethod()
    {
        await Task.Delay(10);
        Console.WriteLine("Test executed!");
    }
}