using System.Reflection;

namespace TUnit.Core.Helpers;

/// <summary>
/// Shared helper for generating consistent test names across reflection and source generation modes
/// </summary>
public static class TestNameGenerator
{
    /// <summary>
    /// Generates a consistent test name using class name and method name
    /// </summary>
    /// <param name="testClass">The test class</param>
    /// <param name="testMethod">The test method</param>
    /// <returns>Test name in format "{ClassName}.{MethodName}"</returns>
    public static string GenerateTestName(Type testClass, MethodInfo testMethod)
    {
        return $"{testClass.Name}.{testMethod.Name}";
    }
    
    /// <summary>
    /// Generates a consistent test name using class name and method name
    /// </summary>
    /// <param name="className">The test class name</param>
    /// <param name="methodName">The test method name</param>
    /// <returns>Test name in format "{ClassName}.{MethodName}"</returns>
    public static string GenerateTestName(string className, string methodName)
    {
        return $"{className}.{methodName}";
    }
}