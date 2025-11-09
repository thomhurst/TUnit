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
        var testDetails = testContext.Metadata.TestDetails ?? throw new ArgumentNullException(nameof(testContext.Metadata.TestDetails));

        var properties = new List<IProperty>
        {
            new TestFileLocationProperty(testDetails.TestFilePath, new LinePositionSpan(
                new LinePosition(testDetails.TestLineNumber, 0),
                new LinePosition(testDetails.TestLineNumber, 0)
            )),
            new TestMethodIdentifierProperty(
                @namespace: testDetails.MethodMetadata.Class.Type.Namespace ?? "",
                assemblyFullName: testDetails.MethodMetadata.Class.Type.Assembly.GetName().FullName,
                typeName: testContext.GetClassTypeName(),
                methodName: testDetails.MethodName,
                parameterTypeFullNames: CreateParameterTypeArray(testDetails.MethodMetadata.Parameters.Select(static p => p.Type).ToArray()),
                returnTypeFullName: testDetails.ReturnType.FullName ?? typeof(void).FullName!,
                methodArity: testDetails.MethodMetadata.GenericTypeCount
            )
        };

        // Custom TUnit Properties
        if (testDetails.Categories.Count > 0)
        {
            properties.AddRange(testDetails.Categories.Select(static category => new TestMetadataProperty(category)));
        }

        if (testDetails.CustomProperties.Count > 0)
        {
            properties.AddRange(ExtractProperties(testDetails));
        }

        // Artifacts
        if(testContext.Output.Artifacts.Count > 0)
        {
            properties.AddRange(testContext.Artifacts.Select(static x => new FileArtifactProperty(x.File, x.DisplayName, x.Description)));
        }

        // TRX Report Properties
        var capabilities = (ITestFrameworkCapabilities)testContext.Services.GetService(typeof(ITestFrameworkCapabilities))!;
        var trxCapability = capabilities.GetCapability<ITrxReportCapability>();
        if (((TrxReportCapability)trxCapability!).IsTrxEnabled)
        {
            properties.Add(new TrxFullyQualifiedTypeNameProperty(testDetails.MethodMetadata.Class.Type.FullName ?? testDetails.ClassType.FullName ?? "UnknownType"));
            properties.Add(new TrxCategoriesProperty([..testDetails.Categories]));
        }

        var testNode = new TestNode
        {
            Uid = new TestNodeUid(testDetails.TestId),
            DisplayName = testContext.GetDisplayName(),
            Properties = new PropertyBag(properties)
        };

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
