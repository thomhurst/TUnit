using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TUnit.Core;
using TUnit.TestAdapter.Constants;

namespace TUnit.TestAdapter.Extensions;

public static class TestExtensions
{
    public static TestCase ToTestCase(this TestDetails testDetails)
    {
        var testCase = new TestCase(testDetails.FullyQualifiedName, TestAdapterConstants.ExecutorUri, testDetails.Source)
        {
            DisplayName = testDetails.DisplayName,
            Id = testDetails.Id,
            CodeFilePath = testDetails.FileName,
            LineNumber = testDetails.MinLineNumber,
            LocalExtensionData = testDetails
        };
        
        testCase.SetPropertyValue(GetOrRegisterTestProperty("ManagedType"), testDetails.FullyQualifiedClassName);
        testCase.SetPropertyValue(GetOrRegisterTestProperty("ManagedMethod"), testDetails.MethodInfo.Name + TestDetails.GetParameterTypes(testDetails.ParameterTypes));
        
        return testCase;
    }

    private static TestProperty GetOrRegisterTestProperty(string name)
    {
        return TestProperty.Find(name) 
               ?? TestProperty.Register(name, name, typeof(string), typeof(TestCase));
    }
}