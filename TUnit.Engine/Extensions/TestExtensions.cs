using System.Reflection;
using Microsoft.TestPlatform.AdapterUtilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TUnit.Core;
using TUnit.Engine.Constants;

namespace TUnit.Engine.Extensions;

internal static class TestExtensions
{
    public static TestInformation ToTestInformation(this TestCase testCase, Type classType, object? classInstance, MethodInfo methodInfo)
    {
        var classParameterTypes =
            testCase.GetPropertyValue(TUnitTestProperties.ClassParameterTypeNames, null as string[]);
        
        var methodParameterTypes =
            testCase.GetPropertyValue(TUnitTestProperties.MethodParameterTypeNames, null as string[]);

        var timeoutMilliseconds = testCase.GetPropertyValue(TUnitTestProperties.Timeout, null as double?);
        
        return new TestInformation
        {
            TestName = testCase.GetPropertyValue(TUnitTestProperties.TestName, ""),
            MethodInfo = methodInfo,
            ClassType = classType,
            ClassInstance = classInstance,
            Categories = testCase.GetPropertyValue(TUnitTestProperties.Category, Array.Empty<string>()).ToList(),
            TestClassArguments = testCase.GetPropertyValue(TUnitTestProperties.ClassArguments, null as string).DeserializeArgumentsSafely(),
            TestMethodArguments = testCase.GetPropertyValue(TUnitTestProperties.MethodArguments, null as string).DeserializeArgumentsSafely(),
            TestClassParameterTypes = classParameterTypes,
            TestMethodParameterTypes = methodParameterTypes,
            Timeout = timeoutMilliseconds is null ? null : TimeSpan.FromMilliseconds(timeoutMilliseconds.Value),
            RepeatCount = testCase.GetPropertyValue(TUnitTestProperties.RepeatCount, 0),
            RetryCount = testCase.GetPropertyValue(TUnitTestProperties.RetryCount, 0),
            NotInParallelConstraintKeys = testCase.GetPropertyValue(TUnitTestProperties.NotInParallelConstraintKeys, null as string[]),
        };
    }

    public static TestCase ToTestCase(this TestDetails testDetails)
    {
        var fullyQualifiedName = GetFullyQualifiedName(testDetails);
        
        var testCase = new TestCase(fullyQualifiedName, TestAdapterConstants.ExecutorUri, testDetails.Source)
        {
            Id = GetId(fullyQualifiedName),
            DisplayName = testDetails.TestNameWithArguments,
            CodeFilePath = testDetails.FileName,
            LineNumber = testDetails.MinLineNumber,
        };

        testCase.SetPropertyValue(TUnitTestProperties.UniqueId, testDetails.UniqueId);

        var testMethodName = testDetails.MethodInfo.Name;
        
        testCase.SetPropertyValue(TUnitTestProperties.TestName, testMethodName);
        testCase.SetPropertyValue(TUnitTestProperties.AssemblyQualifiedClassName, testDetails.ClassType.AssemblyQualifiedName);

        testCase.SetPropertyValueIfNotDefault(TUnitTestProperties.IsSkipped, testDetails.IsSkipped);
        testCase.SetPropertyValueIfNotDefault(TUnitTestProperties.IsStatic, testDetails.MethodInfo.IsStatic);
        
        testCase.SetPropertyValueIfNotDefault(TUnitTestProperties.Category, testDetails.Categories.ToArray());
        
        testCase.SetPropertyValueIfNotDefault(TUnitTestProperties.NotInParallelConstraintKeys, testDetails.NotInParallelConstraintKeys);
        
        testCase.SetPropertyValueIfNotDefault(TUnitTestProperties.Timeout, testDetails.Timeout?.TotalMilliseconds);
        testCase.SetPropertyValueIfNotDefault(TUnitTestProperties.RepeatCount, testDetails.RepeatCount);
        testCase.SetPropertyValueIfNotDefault(TUnitTestProperties.RetryCount, testDetails.RetryCount);

        var testParameterTypes = TestDetails.GetParameterTypes(testDetails.MethodParameterTypes);
        
        var managedMethod = $"{testMethodName}{testParameterTypes}";

        var hierarchy = new[]
        {
            // First option is 'Container' which is empty for C# projects
            string.Empty,
            testDetails.ClassType.Namespace ?? string.Empty,
            testDetails.ClassType.Name,
            testDetails.TestNameWithParameterTypes
        };
        
        testCase.SetPropertyValue(TUnitTestProperties.Hierarchy, hierarchy);
        testCase.SetPropertyValue(TUnitTestProperties.ManagedType, testDetails.FullyQualifiedClassName);
        testCase.SetPropertyValue(TUnitTestProperties.ManagedMethod, managedMethod);
        
        testCase.SetPropertyValueIfNotDefault(TUnitTestProperties.MethodParameterTypeNames, testDetails.MethodParameterTypes?.Select(x => x.FullName).ToArray());
        testCase.SetPropertyValueIfNotDefault(TUnitTestProperties.ClassParameterTypeNames, testDetails.ClassParameterTypes?.Select(x => x.FullName).ToArray());
        testCase.SetPropertyValueIfNotDefault(TUnitTestProperties.MethodArguments, testDetails.MethodArgumentValues.SerializeArgumentsSafely());
        testCase.SetPropertyValueIfNotDefault(TUnitTestProperties.ClassArguments, testDetails.ClassArgumentValues.SerializeArgumentsSafely());
        
        return testCase;
    }

    private static Guid GetId(string fullyQualifiedName)
    {
        var idProvider = new TestIdProvider();
        idProvider.AppendString(fullyQualifiedName);
        return idProvider.GetId();
    }

    private static string GetFullyQualifiedName(TestDetails testDetails)
    {
        if(testDetails.IsSingleTest)
        {
            return $"{testDetails.ClassType.FullName}.{testDetails.TestName}";
        }

        return testDetails.UniqueId;
    }

    private static void SetPropertyValueIfNotDefault<T>(this TestCase testCase, TestProperty property, T value)
    {
        if (EqualityComparer<T>.Default.Equals(value,  default))
        {
            return;
        }
        
        testCase.SetPropertyValue(property, value);
    }
}