using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Engine.Extensions;
using TUnit.Engine.Models.Properties;

namespace TUnit.Engine;

internal class TestClassCreator
{
    public object? CreateClass(TestNode testNode, out Type classType)
    {
        var fullTypeName = testNode.GetRequiredProperty<ClassInformationProperty>().AssemblyQualifiedName;
        
        classType = Type.GetType(fullTypeName, throwOnError: true)!;

        if (testNode.GetRequiredProperty<TestInformationProperty>().IsStatic)
        {
            return null;
        }
        
        var classArguments = testNode.GetRequiredProperty<ClassArgumentsProperty>().Arguments;

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