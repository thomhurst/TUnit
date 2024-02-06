using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TUnit.Core;
using TUnit.TestAdapter.Constants;

namespace TUnit.TestAdapter.Extensions;

internal static class TestExtensions
{
    public static TestInformation ToTestInformation(this TestCase testCase, Type classType, object? classInstance, MethodInfo methodInfo)
    {
        return new TestInformation
        {
            TestName = testCase.GetPropertyValue(TUnitTestProperties.TestName, ""),
            MethodInfo = methodInfo,
            ClassType = classType,
            ClassInstance = classInstance,
            Categories = testCase.GetPropertyValue(TUnitTestProperties.Category, Array.Empty<string>()).ToList(),
            TestClassArguments = testCase.GetPropertyValue(TUnitTestProperties.ClassArguments, null as string).DeserializeArgumentsSafely(),
            TestMethodArguments = testCase.GetPropertyValue(TUnitTestProperties.MethodArguments, null as string).DeserializeArgumentsSafely(),
            Timeout = TimeSpan.FromMilliseconds(testCase.GetPropertyValue(TUnitTestProperties.Timeout, -1d)),
            RepeatCount = testCase.GetPropertyValue(TUnitTestProperties.RepeatCount, 0),
            RetryCount = testCase.GetPropertyValue(TUnitTestProperties.RetryCount, 0),
        };
    }

    public static TestCase ToTestCase(this TestDetails testDetails)
    {
        var testCase = new TestCase(testDetails.UniqueId, TestAdapterConstants.ExecutorUri, testDetails.Source)
        {
            DisplayName = testDetails.DisplayName,
            CodeFilePath = testDetails.FileName,
            LineNumber = testDetails.MinLineNumber,
        };
        
        testCase.SetPropertyValue(TUnitTestProperties.UniqueId, testDetails.UniqueId);
        
        testCase.SetPropertyValue(TUnitTestProperties.TestName, testDetails.MethodInfo.Name);
        testCase.SetPropertyValue(TUnitTestProperties.FullyQualifiedClassName, testDetails.ClassType.FullName);

        testCase.SetPropertyValue(TUnitTestProperties.IsSkipped, testDetails.IsSkipped);
        testCase.SetPropertyValue(TUnitTestProperties.IsStatic, testDetails.MethodInfo.IsStatic);
        
        testCase.SetPropertyValue(TUnitTestProperties.Category, testDetails.Categories.ToArray());
        
        testCase.SetPropertyValue(TUnitTestProperties.NotInParallelConstraintKey, testDetails.NotInParallelConstraintKey);
        
        testCase.SetPropertyValue(TUnitTestProperties.Timeout, testDetails.Timeout.TotalMilliseconds);
        testCase.SetPropertyValue(TUnitTestProperties.RepeatCount, testDetails.RepeatCount);
        testCase.SetPropertyValue(TUnitTestProperties.RetryCount, testDetails.RetryCount);
        
        testCase.SetPropertyValue(TUnitTestProperties.ManagedType, testDetails.FullyQualifiedClassName);
        testCase.SetPropertyValue(TUnitTestProperties.ManagedMethod, testDetails.MethodInfo.Name + TestDetails.GetParameterTypes(testDetails.ParameterTypes));
        
        testCase.SetPropertyValue(TUnitTestProperties.MethodParameterTypeNames, testDetails.ParameterTypes?.Select(x => x.FullName).ToArray());
        testCase.SetPropertyValue(TUnitTestProperties.MethodArguments, testDetails.MethodArgumentValues.SerializeArgumentsSafely());
        testCase.SetPropertyValue(TUnitTestProperties.ClassArguments, testDetails.ClassArgumentValues.SerializeArgumentsSafely());
        
        return testCase;
    }
}