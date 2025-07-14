using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Core.Extensions;

namespace TUnit.Engine.Extensions;

internal static class TestExtensions
{
    internal static TestNode ToTestNode(this TestContext testContext)
    {
        var testDetails = testContext.TestDetails ?? throw new ArgumentNullException(nameof(testContext.TestDetails));

        var testNode = new TestNode
        {
            Uid = new TestNodeUid(testDetails.TestId),
            DisplayName = testContext.GetDisplayName(),
            Properties = new PropertyBag(
            [
                new TestFileLocationProperty(testDetails.TestFilePath, new LinePositionSpan(
                    new LinePosition(testDetails.TestLineNumber, 0),
                    new LinePosition(testDetails.TestLineNumber, 0)
                )),
                new TestMethodIdentifierProperty(
                    Namespace: testDetails.ClassMetadata.Type.Namespace ?? testDetails.ClassType?.Namespace ?? "GlobalNamespace",
                    AssemblyFullName: testDetails.ClassMetadata?.Type.Assembly.FullName ?? testDetails.ClassType?.Assembly.FullName ?? "UnknownAssembly",
                    TypeName: testContext.GetClassTypeName(),
                    MethodName: testDetails.TestName,
                    ParameterTypeFullNames: CreateParameterTypeArray(testDetails.TestMethodParameterTypes),
                    ReturnTypeFullName: testDetails.ReturnType.FullName ?? "void",
                    MethodArity: testDetails.MethodMetadata.GenericTypeCount
                    ),

                // Custom TUnit Properties
                ..testDetails.Categories.Select(category => new TestMetadataProperty(category)),
                ..ExtractProperties(testDetails),

                // Artifacts
                ..testContext.Artifacts.Where(x => x.Value is FileArtifact).Select(x =>
                {
                    var artifact = (FileArtifact)x.Value!;
                    return new FileArtifactProperty(new FileInfo(artifact.File), artifact.DisplayName, artifact.Description);
                }),

                // TRX Report Properties
                new TrxFullyQualifiedTypeNameProperty(testDetails.ClassMetadata?.Type.FullName ?? testDetails.ClassType?.FullName ?? "UnknownType"),
                new TrxCategoriesProperty([..testDetails.Categories]),
            ])
        };

        return testNode;
    }

    public static IEnumerable<KeyValuePairStringProperty> ExtractProperties(this TestDetails testDetails)
    {
        foreach (var propertyGroup in testDetails.CustomProperties)
        {
            foreach (var propertyValue in propertyGroup.Value)
            {
                yield return new KeyValuePairStringProperty(propertyGroup.Key, propertyValue);
            }
        }
    }

    internal static TestNode WithProperty(this TestNode testNode, IProperty property)
    {
        testNode.Properties.Add(property);
        return testNode;
    }

    /// <summary>
    /// Efficiently create parameter type array without LINQ materialization
    /// </summary>
    private static string[] CreateParameterTypeArray(IReadOnlyList<Type>? parameterTypes)
    {
        if (parameterTypes == null || parameterTypes.Count == 0)
            return [];

        var array = new string[parameterTypes.Count];
        for (int i = 0; i < parameterTypes.Count; i++)
        {
            array[i] = parameterTypes[i].FullName!;
        }
        return array;
    }
}
