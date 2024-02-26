using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine;

internal class TestClassCreator
{
    public object? CreateClass(TestNode testNode, out Type classType)
    {
        var fullTypeName = testNode.GetPropertyValue(TUnitTestProperties.AssemblyQualifiedClassName, "");
        
        classType = Type.GetType(fullTypeName, throwOnError: true)!;

        if (testNode.GetPropertyValue(TUnitTestProperties.IsStatic, false))
        {
            return null;
        }
        
        var classArguments = testNode
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