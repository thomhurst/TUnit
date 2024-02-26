using System.Globalization;
using System.Reflection;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;

namespace TUnit.Engine.Extensions;

internal static class TestExtensions
{
    public static TestInformation ToTestInformation(this TestNode testNode, Type classType, object? classInstance, MethodInfo methodInfo)
    {
        var classParameterTypes =
            testNode.GetPropertyValue(TUnitTestProperties.ClassParameterTypeNames, null as string[]);
        
        var methodParameterTypes =
            testNode.GetPropertyValue(TUnitTestProperties.MethodParameterTypeNames, null as string[]);

        var timeoutMilliseconds = testNode.GetPropertyValue(TUnitTestProperties.Timeout, null as double?);
        
        return new TestInformation
        {
            TestName = testNode.GetPropertyValue(TUnitTestProperties.TestName, ""),
            MethodInfo = methodInfo,
            ClassType = classType,
            ClassInstance = classInstance,
            Categories = testNode.GetPropertyValue(TUnitTestProperties.Category, Array.Empty<string>()).ToList(),
            TestClassArguments = testNode.GetPropertyValue(TUnitTestProperties.ClassArguments, null as string).DeserializeArgumentsSafely(),
            TestMethodArguments = testNode.GetPropertyValue(TUnitTestProperties.MethodArguments, null as string).DeserializeArgumentsSafely(),
            TestClassParameterTypes = classParameterTypes,
            TestMethodParameterTypes = methodParameterTypes,
            Timeout = timeoutMilliseconds is null ? null : TimeSpan.FromMilliseconds(timeoutMilliseconds.Value),
            RepeatCount = testNode.GetPropertyValue(TUnitTestProperties.RepeatCount, 0),
            RetryCount = testNode.GetPropertyValue(TUnitTestProperties.RetryCount, 0),
            NotInParallelConstraintKeys = testNode.GetPropertyValue(TUnitTestProperties.NotInParallelConstraintKeys, null as string[]),
            CustomProperties = testNode.Traits.ToDictionary(x => x.Name, x => x.Value).AsReadOnly()
        };
    }

    public static TestNode ToTestNode(this TestDetails testDetails)
    {
        var testNode = new TestNode
        {
            Uid = new TestNodeUid(testDetails.UniqueId),
            DisplayName = testDetails.TestNameWithArguments,
            Properties = new PropertyBag(
            [
                new TestFileLocationProperty(testDetails.FileName!, new LinePositionSpan()
                {
                    Start = new LinePosition(testDetails.MinLineNumber, 0),
                    End = new LinePosition(testDetails.MaxLineNumber, 0)
                }),
                new TestMethodIdentifierProperty(
                    Namespace: testDetails.Namespace,
                    AssemblyFullName: testDetails.Assembly.FullName!,
                    TypeName: testDetails.ClassType.FullName!,
                    MethodName: testDetails.TestName,
                    ParameterTypeFullNames: testDetails.MethodParameterTypes?.Select(x => x.FullName!).ToArray() ?? [],
                    ReturnTypeFullName: testDetails.ReturnType
                    ),
                new DiscoveredTestNodeStateProperty(),
            ])
        };

        if (testDetails.IsSkipped)
        {
            testNode.Properties.Add(new SkippedTestNodeStateProperty());
        }
        
        if(testDetails.ExplicitFor != null)
        {
            testNode.Properties.Add(new TestMetadataProperty(nameof(testDetails.ExplicitFor), testDetails.ExplicitFor));
        }

        testNode.Properties.Add(new TestMetadataProperty(nameof(testDetails.Order), testDetails.Order.ToString()));
        
        testNode.Properties.Add(new TestMetadataProperty(nameof(TUnitTestProperties.IsStatic), testDetails.MethodInfo.IsStatic.ToString()));
        
        testNode.Properties.Add(new TestMetadataProperty(nameof(TUnitTestProperties.Category), testDetails.Categories.ToArray().ToJson()));
        
        if(testDetails.NotInParallelConstraintKeys != null)
        {
            testNode.Properties.Add(new TestMetadataProperty(nameof(TUnitTestProperties.NotInParallelConstraintKeys), testDetails.NotInParallelConstraintKeys.ToJson()));
        }

        testNode.Properties.Add(new TestMetadataProperty(nameof(TUnitTestProperties.Order), testDetails.Order.ToString()));
        
        if(testDetails.Timeout != null)
        {
            testNode.Properties.Add(new TestMetadataProperty(nameof(TUnitTestProperties.Timeout), testDetails.Timeout.Value.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)));
        }

        testNode.Properties.Add(new TestMetadataProperty(nameof(TUnitTestProperties.RepeatCount), testDetails.RepeatCount.ToString()));
        testNode.Properties.Add(new TestMetadataProperty(nameof(TUnitTestProperties.RetryCount), testDetails.RetryCount.ToString()));

        foreach (var customProperty in testDetails.CustomProperties)
        {
            testNode.Properties.Add(new KeyValuePairStringProperty(customProperty.Key, customProperty.Value));
        }
        
        return testNode;
    }

    public static ConstraintKeysCollection GetConstraintKeys(this TestNode testNode)
    {
        var constraintKeys = testNode.GetPropertyValue(TUnitTestProperties.NotInParallelConstraintKeys, null as string[]);
        
        return new ConstraintKeysCollection(
             constraintKeys ?? Array.Empty<string>()
        );
    }
}