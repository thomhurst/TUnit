using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Engine.Models.Properties;

namespace TUnit.Engine.Extensions;

internal static class TestExtensions
{
    public static TestNode ToTestNode(this TestInformation testDetails)
    {
        var testNode = new TestNode
        {
            Uid = new TestNodeUid(testDetails.TestId),
            DisplayName = testDetails.TestName,
            Properties = new PropertyBag(
            [
                new TestFileLocationProperty(testDetails.TestFilePath!, new LinePositionSpan
                {
                    Start = new LinePosition(testDetails.TestLineNumber, 0),
                    End = new LinePosition(testDetails.TestLineNumber, 0)
                }),
                new TestMethodIdentifierProperty(
                    Namespace: testDetails.ClassType.Namespace!,
                    AssemblyFullName: testDetails.ClassType.Assembly.FullName!,
                    TypeName: testDetails.ClassType.FullName!,
                    MethodName: testDetails.TestName,
                    ParameterTypeFullNames: testDetails.TestMethodParameterTypes?.Select(x => x.FullName!).ToArray() ?? [],
                    ReturnTypeFullName: testDetails.ReturnType.FullName!
                    ),
                new TimeoutProperty(testDetails.Timeout ?? TimeSpan.FromMinutes(30)),
                new CategoriesProperty(testDetails.Categories),
                new RepeatCountProperty(testDetails.RepeatCount),
                new RetryCountProperty(testDetails.RetryCount),
                new ClassInformationProperty
                {
                    SimpleName = testDetails.ClassType.Name,
                    FullyQualifiedName = testDetails.ClassType.FullName!,
                    AssemblyQualifiedName = testDetails.ClassType.AssemblyQualifiedName!
                },
                new ClassParameterTypesProperty(testDetails.TestClassParameterTypes?.Select(x => x.FullName!).ToArray()),
                new MethodParameterTypesProperty(testDetails.TestMethodParameterTypes?.Select(x => x.FullName!).ToArray()),
                new ClassArgumentsProperty(testDetails.TestClassArguments),
                new MethodArgumentsProperty(testDetails.TestMethodArguments),
                new NotInParallelConstraintKeysProperty(testDetails.NotInParallelConstraintKeys),
                new OrderProperty(testDetails.Order),
                new TestInformationProperty
                {
                    UniqueId = testDetails.TestId,
                    TestName = testDetails.TestName,
                    IsStatic = testDetails.MethodInfo.IsStatic,
                    ClassExecutionCount = testDetails.ClassRepeatCount,
                    MethodExecutionCount = testDetails.MethodRepeatCount
                },
                new AssemblyProperty(testDetails.ClassType.Assembly.FullName!)
            ])
        };
        
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