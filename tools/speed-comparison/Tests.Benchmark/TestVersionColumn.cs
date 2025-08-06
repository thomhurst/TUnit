using System;

namespace Tests.Benchmark;

public class TestVersionColumn
{
    public static void Main()
    {
        var column = new FrameworkVersionColumn();
        
        Console.WriteLine("Testing Framework Version Column:");
        Console.WriteLine("---------------------------------");
        
        var methods = new[] { "TUnit", "TUnit_AOT", "Build_TUnit", "xUnit", "Build_xUnit", "NUnit", "Build_NUnit", "MSTest", "Build_MSTest" };
        
        foreach (var method in methods)
        {
            // Call the private method through reflection for testing
            var getVersionMethod = typeof(FrameworkVersionColumn).GetMethod("GetFrameworkVersion", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            var version = (string)getVersionMethod.Invoke(null, new object[] { method });
            Console.WriteLine($"{method,-20} : {(string.IsNullOrEmpty(version) ? "(not found)" : version)}");
        }
    }
}