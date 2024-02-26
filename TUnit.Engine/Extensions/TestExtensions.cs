using System.Reflection;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Engine.Models.Properties;

namespace TUnit.Engine.Extensions;

internal static class TestExtensions
{
    public static TestInformation ToTestInformation(this TestNode testNode, Type classType, object? classInstance, MethodInfo methodInfo)
    {
        return new TestInformation
        {
            TestName = testNode.GetRequiredProperty<TestInformationProperty>().TestName,
            MethodInfo = methodInfo,
            ClassType = classType,
            ClassInstance = classInstance,
            Categories = testNode.GetRequiredProperty<CategoriesProperty>().Categories ?? [],
            TestClassArguments = testNode.GetRequiredProperty<ClassArgumentsProperty>().Arguments,
            TestMethodArguments = testNode.GetRequiredProperty<MethodArgumentsProperty>().Arguments,
            TestClassParameterTypes = testNode.GetRequiredProperty<ClassParameterTypesProperty>().FullyQualifiedTypeNames?.Select(Type.GetType).ToArray(),
            TestMethodParameterTypes = testNode.GetRequiredProperty<MethodParameterTypesProperty>().FullyQualifiedTypeNames?.Select(Type.GetType).ToArray(),
            Timeout = testNode.GetRequiredProperty<TimeoutProperty>().Timeout,
            RepeatCount = testNode.GetRequiredProperty<RepeatCountProperty>().Count,
            RetryCount = testNode.GetRequiredProperty<RetryCountProperty>().Count,
            NotInParallelConstraintKeys = testNode.GetRequiredProperty<NotInParallelConstraintKeysProperty>().ConstraintKeys?.ToArray(),
            CustomProperties = testNode.Properties.OfType<CustomProperty>().ToDictionary(x => x.Key, x => x.Value).AsReadOnly()
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
                new TestFileLocationProperty(testDetails.FileName!, new LinePositionSpan
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
                new TimeoutProperty(testDetails.Timeout ?? TimeSpan.FromMinutes(30)),
                new CategoriesProperty(testDetails.Categories),
                new RepeatCountProperty(testDetails.RepeatCount),
                new RetryCountProperty(testDetails.RetryCount),
                new ClassInformationProperty(testDetails.ClassName, testDetails.FullyQualifiedClassName, testDetails.AssemblyQualifiedClassName),
                new ClassParameterTypesProperty(testDetails.ClassParameterTypes?.Select(x => x.FullName!).ToArray()),
                new MethodParameterTypesProperty(testDetails.MethodParameterTypes?.Select(x => x.FullName!).ToArray()),
                new ClassArgumentsProperty(testDetails.ClassArgumentValues),
                new MethodArgumentsProperty(testDetails.MethodArgumentValues),
                new NotInParallelConstraintKeysProperty(testDetails.NotInParallelConstraintKeys),
                new OrderProperty(testDetails.Order),
                new TestInformationProperty(testDetails.UniqueId, testDetails.TestName, testDetails.MethodInfo.IsStatic, testDetails.IsSingleTest),
                new AssemblyProperty(testDetails.Assembly.FullName!)
            ])
        };

        if (testDetails.IsSkipped)
        {
            testNode.Properties.Add(new SkipReasonProperty(testDetails.SkipReason ?? string.Empty));
        }
        
        if(testDetails.ExplicitFor != null)
        {
            testNode.Properties.Add(new ExplicitForProperty(testDetails.ExplicitFor));
        }
        
        foreach (var customProperty in testDetails.CustomProperties)
        {
            testNode.Properties.Add(new CustomProperty(customProperty.Key, customProperty.Value));
        }
        
        return testNode;
    }

    public static ConstraintKeysCollection GetConstraintKeys(this TestNode testNode)
    {
        var constraintKeys = testNode.GetRequiredProperty<NotInParallelConstraintKeysProperty>().ConstraintKeys;
        
        return new ConstraintKeysCollection(
             constraintKeys ?? Array.Empty<string>()
        );
    }

    public static string GetPropertyValue(this TestNode testNode, string key)
    {
        return testNode.Properties
            .OfType<TestMetadataProperty>()
            .First(x => x.Key == key)
            .Value;
    }
    
    public static T GetRequiredProperty<T>(this TestNode testNode) where T : IProperty
    {
        return testNode.Properties.OfType<T>().First();
    }

    public static T? GetProperty<T>(this TestNode testNode) where T : IProperty
    {
        return testNode.Properties.OfType<T>().FirstOrDefault();
    }
}