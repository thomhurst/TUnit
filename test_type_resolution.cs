using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        // Test various ways to get the generic type
        Console.WriteLine("Testing Type.GetType resolution...");
        
        // Method 1: Using assembly-qualified name
        var type1 = Type.GetType("TUnit.TestProject.SimpleGenericClassTests`1, TUnit.TestProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        Console.WriteLine($"Method 1 (with assembly info): {type1}");
        
        // Method 2: Without assembly version info
        var type2 = Type.GetType("TUnit.TestProject.SimpleGenericClassTests`1, TUnit.TestProject");
        Console.WriteLine($"Method 2 (simple assembly name): {type2}");
        
        // Method 3: Get the loaded assembly first
        var assembly = Assembly.Load("TUnit.TestProject");
        var type3 = assembly.GetType("TUnit.TestProject.SimpleGenericClassTests`1");
        Console.WriteLine($"Method 3 (from loaded assembly): {type3}");
        
        // Method 4: Without global:: prefix
        var type4 = Type.GetType("TUnit.TestProject.SimpleGenericClassTests`1, TUnit.TestProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        Console.WriteLine($"Method 4 (without global::): {type4}");
    }
}