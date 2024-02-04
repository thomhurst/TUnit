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
            CodeFilePath = testDetails.FileName,
            LineNumber = testDetails.MinLineNumber,
            LocalExtensionData = testDetails
        };
        
        testCase.SetPropertyValue(GetOrRegisterTestProperty<string>(nameof(TestDetails.UniqueId)), testDetails.UniqueId);
        
        testCase.SetPropertyValue(GetOrRegisterTestProperty<string>("ManagedType"), testDetails.FullyQualifiedClassName);
        testCase.SetPropertyValue(GetOrRegisterTestProperty<string>("ManagedMethod"), testDetails.MethodInfo.Name + TestDetails.GetParameterTypes(testDetails.ParameterTypes));
        
        return testCase;
    }

    private static TestProperty GetOrRegisterTestProperty<T>(string name)
    {
        return TestProperty.Find(name) 
               ?? TestProperty.Register(name, name, typeof(T), typeof(TestCase));
    }
}