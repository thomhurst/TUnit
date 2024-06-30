using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Engine.Properties;

namespace TUnit.Engine.Extensions;

internal static class TestExtensions
{
    public static TestNode ToTestNode(this TestInformation testDetails)
    {
        var testNode = new TestNode
        {
            Uid = new TestNodeUid(testDetails.TestId),
            DisplayName = testDetails.DisplayName,
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
                    TypeName: testDetails.ClassType.Name,
                    MethodName: testDetails.TestName,
                    ParameterTypeFullNames: testDetails.TestMethodParameterTypes?.Select(x => x.FullName!).ToArray() ?? [],
                    ReturnTypeFullName: testDetails.ReturnType.FullName!
                    ),
                
                // Custom TUnit Properties
                ..testDetails.Categories.Select(x => new CategoryProperty(x)),
                ..testDetails.CustomProperties.Select(x => new KeyValuePairStringProperty(x.Key, x.Value)),
                
                // TRX Report Properties
                new TrxFullyQualifiedTypeNameProperty(testDetails.ClassType.FullName!),
                new TrxCategoriesProperty([..testDetails.Categories]),
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
    
    public static TestNode WithProperty(this TestNode testNode, IProperty property)
    {
        testNode.Properties.Add(property);
        return testNode;
    }
}