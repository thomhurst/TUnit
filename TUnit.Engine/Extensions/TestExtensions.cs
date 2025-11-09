using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Engine.Capabilities;
#pragma warning disable TPEXP

namespace TUnit.Engine.Extensions;

internal static class TestExtensions
{
    internal static TestNode ToTestNode(this TestContext testContext, TestNodeStateProperty stateProperty)
    {
        var testDetails = testContext.Metadata.TestDetails ?? throw new ArgumentNullException(nameof(testContext.Metadata.TestDetails));

        var isFinalState = stateProperty is not DiscoveredTestNodeStateProperty and not InProgressTestNodeStateProperty;

        var capabilities = (ITestFrameworkCapabilities)testContext.Services.GetService(typeof(ITestFrameworkCapabilities))!;
        var trxCapability = capabilities.GetCapability<ITrxReportCapability>();
        var isTrxEnabled = ((TrxReportCapability)trxCapability!).IsTrxEnabled;

        var estimatedCount =
                3 + // State + FileLocation + MethodIdentifier
                testDetails.Categories.Count +
                testDetails.CustomProperties.Count +
                testContext.Artifacts.Count +
                (isFinalState ? 3 : 0) + // Timing + StdOut + StdErr;
                (isTrxEnabled ? 3 : 0) // TRX TypeName + TRX Categories + TRX Messages
            ;

        var properties = new List<IProperty>(estimatedCount)
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

        string? output = null;
        if (isFinalState && testContext.GetStandardOutput() is {} standardOutput && !string.IsNullOrEmpty(standardOutput))
        {
            output = standardOutput;
            properties.Add(new StandardOutputProperty(standardOutput));
        }

        string? error = null;
        if (isFinalState && testContext.GetErrorOutput() is {} standardError && !string.IsNullOrEmpty(standardError))
        {
            error = standardError;
            properties.Add(new StandardErrorProperty(standardError));
        }

        // TRX Report Properties
        if (isTrxEnabled)
        {
            properties.Add(new TrxFullyQualifiedTypeNameProperty(testDetails.MethodMetadata.Class.Type.FullName ?? testDetails.ClassType.FullName ?? "UnknownType"));

            if(testDetails.Categories.Count > 0)
            {
                properties.Add(new TrxCategoriesProperty([..testDetails.Categories]));
            }

            if (isFinalState && GetTrxMessages(testContext, output, error).ToArray() is { Length: > 0 } trxMessages)
            {
                properties.Add(new TrxMessagesProperty(trxMessages));
            }
        }

        if(isFinalState)
        {
            properties.Add(GetTimingProperty(testContext, testContext.Execution.TestStart.GetValueOrDefault()));
        }

        var testNode = new TestNode
        {
            Uid = new TestNodeUid(testDetails.TestId),
            DisplayName = testContext.GetDisplayName(),
            Properties = new PropertyBag(properties)
        };

        return testNode;
    }

    private static IEnumerable<TestMetadataProperty> ExtractProperties(TestDetails testDetails)
    {
        foreach (var propertyGroup in testDetails.CustomProperties)
        {
            foreach (var propertyValue in propertyGroup.Value)
            {
                yield return new TestMetadataProperty(propertyGroup.Key, propertyValue);
            }
        }
    }

    private static TimingProperty GetTimingProperty(TestContext testContext, DateTimeOffset overallStart)
    {
        if (overallStart == default(DateTimeOffset))
        {
            return new TimingProperty(new TimingInfo());
        }

        var end = DateTimeOffset.Now;

        return new TimingProperty(new TimingInfo(overallStart, end, end - overallStart), testContext.Timings.Select(x => new StepTimingInfo(x.StepName, x.StepName, new TimingInfo(x.Start, x.End, x.Duration))).ToArray());
    }

    private static IEnumerable<TrxMessage> GetTrxMessages(TestContext testContext, string? standardOutput, string? standardError)
    {
        if (!string.IsNullOrEmpty(standardOutput))
        {
            yield return new StandardOutputTrxMessage(standardOutput);
        }

        if (!string.IsNullOrEmpty(standardError))
        {
            yield return new StandardErrorTrxMessage(standardError);
        }

        if (!string.IsNullOrEmpty(testContext.SkipReason))
        {
            yield return new DebugOrTraceTrxMessage($"Skipped: {testContext.SkipReason}");
        }
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
