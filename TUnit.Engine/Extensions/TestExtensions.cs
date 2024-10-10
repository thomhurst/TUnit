using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;

namespace TUnit.Engine.Extensions;

internal static class TestExtensions
{
    public static TestNode ToTestNode(this TestDetails testDetails)
    {
        var testNode = new TestNode
        {
            Uid = new TestNodeUid(testDetails.TestId),
            DisplayName = testDetails.DisplayName,
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
                    TypeName: GetClassTypeName(testDetails),
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

    private static string GetClassTypeName(TestDetails testDetails)
    {
        var classTypeName = testDetails.ClassType.FullName?
                                .Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                                .LastOrDefault()
            ?? testDetails.ClassType.Name;
        
        if (testDetails.TestClassArguments.Length == 0)
        {
            return classTypeName;
        }
        
        return
            $"{classTypeName}({string.Join(", ", testDetails.TestClassArguments.Select(GetConstantValue))})";
    }

    private static string GetConstantValue(object? o)
    {
        if (o is null)
        {
            return "null";
        }
        
        if (o.GetType().IsEnum || o.GetType().IsPrimitive || o is string)
        {
            return o.ToString()!;
        }

        return o.GetType().Name;
    }

    public static TestNode WithProperty(this TestNode testNode, IProperty property)
    {
        testNode.Properties.Add(property);
        return testNode;
    }
}