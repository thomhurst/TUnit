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
            TestClassParameterTypes = testNode.GetRequiredProperty<ClassParameterTypesProperty>().FullyQualifiedTypeNames?.Select(Type.GetType).OfType<Type>().ToArray(),
            TestMethodParameterTypes = testNode.GetRequiredProperty<MethodParameterTypesProperty>().FullyQualifiedTypeNames?.Select(Type.GetType).OfType<Type>().ToArray(),
            Timeout = testNode.GetRequiredProperty<TimeoutProperty>().Timeout,
            RepeatCount = testNode.GetRequiredProperty<RepeatCountProperty>().Count,
            RetryCount = testNode.GetRequiredProperty<RetryCountProperty>().Count,
            NotInParallelConstraintKeys = testNode.GetRequiredProperty<NotInParallelConstraintKeysProperty>().ConstraintKeys?.ToArray(),
            CustomProperties = testNode.Properties.OfType<CustomProperty>().ToDictionary(x => x.Key, x => x.Value).AsReadOnly(),
            MethodRepeatCount = testNode.GetRequiredProperty<TestInformationProperty>().MethodExecutionCount,
            ClassRepeatCount = testNode.GetRequiredProperty<TestInformationProperty>().ClassExecutionCount,
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
                new TimeoutProperty(testDetails.Timeout ?? TimeSpan.FromMinutes(30)),
                new CategoriesProperty(testDetails.Categories),
                new RepeatCountProperty(testDetails.RepeatCount),
                new RetryCountProperty(testDetails.RetryCount),
                new ClassInformationProperty
                {
                    SimpleName = testDetails.ClassName, 
                    FullyQualifiedName = testDetails.FullyQualifiedClassName, 
                    AssemblyQualifiedName = testDetails.AssemblyQualifiedClassName
                },
                new ClassParameterTypesProperty(testDetails.ClassParameterTypes?.Select(x => x.FullName!).ToArray()),
                new MethodParameterTypesProperty(testDetails.MethodParameterTypes?.Select(x => x.FullName!).ToArray()),
                new ClassArgumentsProperty(testDetails.ClassArgumentValues),
                new MethodArgumentsProperty(testDetails.MethodArgumentValues),
                new NotInParallelConstraintKeysProperty(testDetails.NotInParallelConstraintKeys),
                new OrderProperty(testDetails.Order),
                new TestInformationProperty
                {
                    UniqueId = testDetails.UniqueId, 
                    TestName = testDetails.TestName, 
                    IsStatic = testDetails.MethodInfo.IsStatic, 
                    IsSingleTest = testDetails.IsSingleTest,
                    ClassExecutionCount = testDetails.CurrentClassRepeatCount,
                    MethodExecutionCount = testDetails.CurrentMethodRepeatCount
                },
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

    public static T GetRequiredProperty<T>(this TestNode testNode) where T : IProperty
    {
        return testNode.Properties.Single<T>();
    }

    public static T? GetProperty<T>(this TestNode testNode) where T : IProperty
    {
        return testNode.Properties.SingleOrDefault<T>();
    }
}