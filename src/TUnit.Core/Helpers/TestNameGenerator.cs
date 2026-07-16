using System.Reflection;

namespace TUnit.Core.Helpers;

public static class TestNameGenerator
{
    public static string GenerateTestName(Type testClass, MethodInfo testMethod)
    {
        return $"{testClass.Name}.{testMethod.Name}";
    }
    
    public static string GenerateTestName(string className, string methodName)
    {
        return $"{className}.{methodName}";
    }
}