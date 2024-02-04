using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TUnit.Core;
using TUnit.TestAdapter.Constants;

namespace TUnit.TestAdapter.Extensions;

internal static class TestExtensions
{
    public static TestCase ToTestCase(this TestDetails testDetails)
    {
        var testCase = new TestCase(testDetails.UniqueId, TestAdapterConstants.ExecutorUri, testDetails.Source)
        {
            DisplayName = testDetails.DisplayName,
            CodeFilePath = testDetails.FileName,
            LineNumber = testDetails.MinLineNumber,
        };
        
        testCase.SetPropertyValue(TUnitTestProperties.UniqueId, testDetails.UniqueId);
        
        testCase.SetPropertyValue(TUnitTestProperties.ManagedType, testDetails.FullyQualifiedClassName);
        testCase.SetPropertyValue(TUnitTestProperties.ManagedMethod, testDetails.MethodInfo.Name + TestDetails.GetParameterTypes(testDetails.ParameterTypes));
        
        return testCase;
    }
}