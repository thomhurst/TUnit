using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TUnit.Core;
using TUnit.TestAdapter.Constants;

namespace TUnit.TestAdapter.Extensions;

public static class TestExtensions
{
    public static TestCase ToTestCase(this Test test)
    {
        var testCase = new TestCase(test.FullyQualifiedName, TestAdapterConstants.ExecutorUri, test.Source)
        {
            DisplayName = test.DisplayName,
            Id = test.Id,
            CodeFilePath = test.FileName,
            LineNumber = test.MinLineNumber,
        };
        
        testCase.SetPropertyValue(GetOrRegisterTestProperty("ManagedType"), test.FullyQualifiedClassName);
        testCase.SetPropertyValue(GetOrRegisterTestProperty("ManagedMethod"), test.MethodInfo.Name + Test.GetParameterTypes(test.ParameterTypes));
        
        return testCase;
    }

    private static TestProperty GetOrRegisterTestProperty(string name)
    {
        return TestProperty.Find(name) 
               ?? TestProperty.Register(name, name, typeof(string), typeof(TestCase));
    }
}