using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Engine.Capabilities;

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
                    Namespace: testDetails.MethodMetadata.Class.Type.Namespace ?? "",
                    AssemblyFullName: testDetails.MethodMetadata.Class.Type.Assembly.GetName().FullName,
                    TypeName: testContext.GetClassTypeName(),
                    MethodName: testDetails.MethodName,
                    ParameterTypeFullNames: CreateParameterTypeArray(testDetails.MethodMetadata.Parameters.Select(p => p.Type).ToArray()),
                    ReturnTypeFullName: testDetails.ReturnType.FullName ?? typeof(void).FullName!,
                    MethodArity: testDetails.MethodMetadata.GenericTypeCount
                    ),

                // Custom TUnit Properties
                ..testDetails.Categories.Select(category => new TestMetadataProperty(category)),
                ..ExtractProperties(testDetails),

                // Artifacts
                ..testContext.Artifacts.Select(x => new FileArtifactProperty(x.File, x.DisplayName, x.Description)),                
            ])
        };

        var trxCapability = testContext.GetService<ITestFrameworkCapabilities>().GetCapability<ITrxReportCapability>();
        if (((TrxReportCapability)trxCapability!).IsTrxEnabled)
        {
            // TRX Report Properties
            testNode.WithProperty(new TrxFullyQualifiedTypeNameProperty(testDetails.MethodMetadata.Class?.Type.FullName ?? testDetails.ClassType?.FullName ?? "UnknownType"))
                .WithProperty(new TrxCategoriesProperty([.. testDetails.Categories]));
        }
        
        return testNode;
    }

    public static IEnumerable<TestMetadataProperty> ExtractProperties(this TestDetails testDetails)
    {
        foreach (var propertyGroup in testDetails.CustomProperties)
        {
            foreach (var propertyValue in propertyGroup.Value)
            {
                yield return new TestMetadataProperty(propertyGroup.Key, propertyValue);
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
        {
            return [];
        }

        var array = new string[parameterTypes.Count];
        for (var i = 0; i < parameterTypes.Count; i++)
        {
            array[i] = parameterTypes[i].FullName!;
        }
        return array;
    }
}
