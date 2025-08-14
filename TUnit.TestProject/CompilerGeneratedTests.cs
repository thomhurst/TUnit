using System;
using System.Runtime.CompilerServices;
using TUnit.Core;

namespace TUnit.TestProject;

// This simulates a Reqnroll-generated test class with CompilerGeneratedAttribute
[CompilerGenerated]
public class CompilerGeneratedTests
{
    [Test]
    public void GeneratedTestMethod()
    {
        // This test should be executed even though the class is marked as CompilerGenerated
        Console.WriteLine("Generated test executed successfully");
    }
    
    [Test]
    [Arguments(1, 2, 3)]
    [Arguments(2, 3, 5)]
    public void GeneratedTestWithArguments(int a, int b, int expected)
    {
        var result = a + b;
        if (result != expected)
        {
            throw new Exception($"Expected {expected}, but got {result}");
        }
        Console.WriteLine($"Generated test with arguments: {a} + {b} = {result}");
    }
}

// Test that types without test methods are still filtered out
[CompilerGenerated]
public class CompilerGeneratedNonTestClass
{
    public void NotATestMethod()
    {
        // This class should still be filtered out as it has no test methods
    }
}