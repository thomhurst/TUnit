using System;
using System.Reflection;
using System.Threading.Tasks;

// Simulate what the generated code does
public class SimpleGenericClassTests<T>
{
    public async Task TestWithValue(T value)
    {
        Console.WriteLine($"TestWithValue called with value: {value} of type {typeof(T)}");
        await Task.CompletedTask;
    }
}

class Program
{
    static async Task Main()
    {
        // Simulate runtime generic type resolution
        var resolvedTypes = new System.Collections.Generic.Dictionary<string, Type>
        {
            ["T"] = typeof(int)
        };
        
        // Step 1: Construct the generic type
        var genericTypeDef = Type.GetType("SimpleGenericClassTests`1");
        if (genericTypeDef == null)
        {
            Console.WriteLine("Error: Could not find generic type definition");
            return;
        }
        
        var typeArguments = new Type[] { resolvedTypes["T"] };
        var constructedType = genericTypeDef.MakeGenericType(typeArguments);
        Console.WriteLine($"Constructed type: {constructedType}");
        
        // Step 2: Create instance
        var instance = Activator.CreateInstance(constructedType);
        Console.WriteLine($"Created instance: {instance}");
        
        // Step 3: Get and invoke method
        var methodInfo = constructedType.GetMethod("TestWithValue", BindingFlags.Public | BindingFlags.Instance);
        Console.WriteLine($"Found method: {methodInfo}");
        
        // Step 4: Invoke with arguments
        var args = new object[] { 42 };
        var result = methodInfo.Invoke(instance, args);
        
        if (result is Task task)
        {
            await task;
            Console.WriteLine("Task completed successfully!");
        }
    }
}