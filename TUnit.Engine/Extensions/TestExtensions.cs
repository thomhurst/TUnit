using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Core.Extensions;

namespace TUnit.Engine.Extensions;

internal static class TestExtensions
{
    internal static TestNode ToTestNode(this TestContext testContext)
    {
        var testDetails = testContext.TestDetails;
        
        var testNode = new TestNode
        {
            Uid = new TestNodeUid(testDetails.TestId),
            DisplayName = testContext.GetTestDisplayName(),
            Properties = new PropertyBag(
            [
                new TestFileLocationProperty(testDetails.TestFilePath, new LinePositionSpan
                {
                    Start = new LinePosition(testDetails.TestLineNumber, 0),
                    End = new LinePosition(testDetails.TestLineNumber, 0)
                }),
                new TestMethodIdentifierProperty(
                    Namespace: testDetails.ClassType.Namespace!,
                    AssemblyFullName: testDetails.ClassType.Assembly.FullName!,
                    TypeName: testContext.GetClassTypeName(),
                    MethodName: testDetails.TestName,
                    ParameterTypeFullNames: testDetails.TestMethodParameterTypes.Select(x => x.FullName!).ToArray(),
                    ReturnTypeFullName: testDetails.ReturnType.FullName!
                    ),
                
                // Custom TUnit Properties
                ..testDetails.Categories.Select(x => new KeyValuePairStringProperty("Category", x)),
                ..testDetails.CustomProperties.Select(x => new KeyValuePairStringProperty(x.Key, x.Value)),
                
                // TRX Report Properties
                new TrxFullyQualifiedTypeNameProperty(testDetails.ClassType.FullName!),
                new TrxCategoriesProperty([..testDetails.Categories]),
            ])
        };
        
        return testNode;
    }

    internal static TestNode WithProperty(this TestNode testNode, IProperty property)
    {
        testNode.Properties.Add(property);
        return testNode;
    }
}