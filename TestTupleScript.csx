#r "D:\git\TUnit\TUnit.Core\bin\Debug\net8.0\TUnit.Core.dll"

using System;
using System.Linq;
using TUnit.Core.Helpers;

Console.WriteLine("Testing tuple unwrapping with DataSourceHelpers...");

// Test 1: Simple tuple
var tuple1 = (42, "Hello");
var result1 = DataSourceHelpers.ProcessTestDataSource(tuple1);
Console.WriteLine($"Tuple (42, \"Hello\") -> {result1.Length} factories");

// Test 2: Nested tuple in array (simulating ClassDataSource return)
var dataArray = new object?[] { (99, "World") };
var firstElement = dataArray[0];
var unwrapped = DataSourceHelpers.UnwrapTupleAot(firstElement);
Console.WriteLine($"\nArray with tuple -> {unwrapped.Length} elements:");
for (int i = 0; i < unwrapped.Length; i++)
{
    Console.WriteLine($"  [{i}] = {unwrapped[i]}");
}

// Test 3: ProcessTestDataSource with the tuple directly
var factories = DataSourceHelpers.ProcessTestDataSource((123, "Test"));
Console.WriteLine($"\nProcessTestDataSource on tuple -> {factories.Length} factories");

// Test 4: Get values from factories
Console.WriteLine("\nGetting values from factories:");
for (int i = 0; i < factories.Length; i++)
{
    var value = await factories[i]();
    Console.WriteLine($"  Factory[{i}] -> {value}");
}

Console.WriteLine("\nAll tests completed!");