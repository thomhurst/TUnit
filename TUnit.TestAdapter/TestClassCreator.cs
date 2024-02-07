using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TUnit.Engine;
using TUnit.TestAdapter.Extensions;

namespace TUnit.TestAdapter;

internal class TestClassCreator(CacheableAssemblyLoader assemblyLoader)
{
    public object? CreateClass(TestCase testCase, out Type classType)
    {
        var fullTypeName = testCase.GetPropertyValue(TUnitTestProperties.AssemblyQualifiedClassName, "");
        
        classType = Type.GetType(fullTypeName, throwOnError: true)!;

        if (testCase.GetPropertyValue(TUnitTestProperties.IsStatic, false))
        {
            return null;
        }
        
        var classArguments = testCase
            .GetPropertyValue(TUnitTestProperties.ClassArguments, null as string)
            .DeserializeArgumentsSafely();

        return CreateClass(classType, classArguments);
    }

    public object CreateClass(Type classType, object?[]? arguments)
    {
        try
        {
            return Activator.CreateInstance(classType, arguments)!;
        }
        catch (Exception e)
        {
            throw new Exception($"""
                                Cannot create an instance of the test class.
                                   {e.Message}
                                """, e);
        }
    }
}