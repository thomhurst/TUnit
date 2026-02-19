using System.Collections.Concurrent;
using System.Reflection;
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
    private static bool? _cachedIsTrxEnabled;

    private static readonly ConcurrentDictionary<Assembly, string> AssemblyFullNameCache = new();
    private static readonly ConcurrentDictionary<string, CachedTestNodeProperties> TestNodePropertiesCache = new();

    private sealed class CachedTestNodeProperties
    {
        public required TestFileLocationProperty FileLocation { get; init; }
        public required TestMethodIdentifierProperty MethodIdentifier { get; init; }
        public TestMetadataProperty[]? CategoryProperties { get; init; }
        public TestMetadataProperty[]? TagProperties { get; init; }
        public TestMetadataProperty[]? CustomProperties { get; init; }
        public string? TrxFullyQualifiedTypeName { get; init; }
        public TrxCategoriesProperty? TrxCategories { get; init; }
    }

    internal static void ClearCaches()
    {
        AssemblyFullNameCache.Clear();
        TestNodePropertiesCache.Clear();
        _cachedIsTrxEnabled = null;
    }

    private static string GetCachedAssemblyFullName(Assembly assembly)
    {
        return AssemblyFullNameCache.GetOrAdd(assembly, static a => a.GetName().FullName);
    }

    private static CachedTestNodeProperties GetOrCreateCachedProperties(TestContext testContext)
    {
        var testId = testContext.Metadata.TestDetails.TestId;

        return TestNodePropertiesCache.GetOrAdd(testId, static (_, testContext) =>
        {
            var testDetails = testContext.Metadata.TestDetails;

            var fileLocation = new TestFileLocationProperty(testDetails.TestFilePath, new LinePositionSpan(
                new LinePosition(testDetails.TestLineNumber, 0),
                new LinePosition(testDetails.TestLineNumber, 0)
            ));

            var methodIdentifier = new TestMethodIdentifierProperty(
                @namespace: testDetails.MethodMetadata.Class.Type.Namespace ?? "",
                assemblyFullName: GetCachedAssemblyFullName(testDetails.MethodMetadata.Class.Type.Assembly),
                typeName: testContext.GetClassTypeName(),
                methodName: testDetails.MethodName,
                parameterTypeFullNames: CreateParameterTypeArray(testDetails.MethodMetadata.Parameters),
                returnTypeFullName: testDetails.ReturnType.FullName ?? typeof(void).FullName!,
                methodArity: testDetails.MethodMetadata.GenericTypeCount
            );

            TestMetadataProperty[]? categoryProps = null;
            if (testDetails.Categories.Count > 0)
            {
                categoryProps = new TestMetadataProperty[testDetails.Categories.Count];
                for (var i = 0; i < testDetails.Categories.Count; i++)
                {
                    categoryProps[i] = new TestMetadataProperty(testDetails.Categories[i]);
                }
            }

            TestMetadataProperty[]? tagProps = null;
            if (testDetails.Tags.Count > 0)
            {
                tagProps = new TestMetadataProperty[testDetails.Tags.Count];
                for (var i = 0; i < testDetails.Tags.Count; i++)
                {
                    tagProps[i] = new TestMetadataProperty("Tag", testDetails.Tags[i]);
                }
            }

            TestMetadataProperty[]? customProps = null;
            if (testDetails.CustomProperties.Count > 0)
            {
                var count = 0;
                foreach (var prop in testDetails.CustomProperties)
                {
                    count += prop.Value.Count;
                }

                customProps = new TestMetadataProperty[count];
                var idx = 0;
                foreach (var prop in testDetails.CustomProperties)
                {
                    foreach (var value in prop.Value)
                    {
                        customProps[idx++] = new TestMetadataProperty(prop.Key, value);
                    }
                }
            }

            var trxTypeName = testDetails.MethodMetadata.Class.Type.FullName ?? testDetails.ClassType.FullName ?? "UnknownType";

            TrxCategoriesProperty? trxCategories = null;
            if (testDetails.Categories.Count > 0)
            {
                trxCategories = new TrxCategoriesProperty([..testDetails.Categories]);
            }

            return new CachedTestNodeProperties
            {
                FileLocation = fileLocation,
                MethodIdentifier = methodIdentifier,
                CategoryProperties = categoryProps,
                TagProperties = tagProps,
                CustomProperties = customProps,
                TrxFullyQualifiedTypeName = trxTypeName,
                TrxCategories = trxCategories
            };
        }, testContext);
    }

    internal static TestNode ToTestNode(this TestContext testContext, TestNodeStateProperty stateProperty)
    {
        var testDetails = testContext.Metadata.TestDetails ?? throw new ArgumentNullException(nameof(testContext.Metadata.TestDetails));

        var isFinalState = stateProperty is not DiscoveredTestNodeStateProperty and not InProgressTestNodeStateProperty;

        var isTrxEnabled = isFinalState && IsTrxEnabled(testContext);

        var cachedProps = GetOrCreateCachedProperties(testContext);

        var estimatedCount = EstimateCount(testContext, stateProperty, isTrxEnabled);

        var properties = new List<IProperty>(estimatedCount)
        {
            stateProperty,
            cachedProps.FileLocation,
            cachedProps.MethodIdentifier
        };

        if (cachedProps.CategoryProperties != null)
        {
            properties.AddRange(cachedProps.CategoryProperties);
        }

        if (cachedProps.TagProperties != null)
        {
            properties.AddRange(cachedProps.TagProperties);
        }

        if (cachedProps.CustomProperties != null)
        {
            properties.AddRange(cachedProps.CustomProperties);
        }

        if (isFinalState && testContext.Output.Artifacts.Count > 0)
        {
            foreach (var artifact in testContext.Artifacts)
            {
                properties.Add(new FileArtifactProperty(artifact.File, artifact.DisplayName, artifact.Description));
            }
        }

        string? output = null;
        string? error = null;

        if (isFinalState)
        {
            output = testContext.GetStandardOutput();
            error = testContext.GetErrorOutput();

            if (!string.IsNullOrEmpty(output))
            {
                properties.Add(new StandardOutputProperty(output));
            }

            if (!string.IsNullOrEmpty(error))
            {
                properties.Add(new StandardErrorProperty(error));
            }
        }

        if (isFinalState && isTrxEnabled)
        {
            properties.Add(new TrxFullyQualifiedTypeNameProperty(cachedProps.TrxFullyQualifiedTypeName!));

            if (cachedProps.TrxCategories != null)
            {
                properties.Add(cachedProps.TrxCategories);
            }

            if (GetTrxMessages(testContext, output, error).ToArray() is { Length: > 0 } trxMessages)
            {
                properties.Add(new TrxMessagesProperty(trxMessages));
            }

            if (stateProperty is ErrorTestNodeStateProperty or FailedTestNodeStateProperty or TimeoutTestNodeStateProperty)
            {
                var (exception, explanation) = GetException(stateProperty);

                if (exception is not null)
                {
                    properties.Add(new TrxExceptionProperty(exception.Message, exception.StackTrace));
                }
                else if (!string.IsNullOrEmpty(explanation))
                {
                    properties.Add(new TrxExceptionProperty(explanation, string.Empty));
                }
            }
        }

        if (isFinalState)
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

    private static (Exception? Exception, string? Reason) GetException(TestNodeStateProperty stateProperty)
    {
        if (stateProperty is ErrorTestNodeStateProperty errorState)
        {
            return (errorState.Exception, errorState.Explanation);
        }

        if (stateProperty is FailedTestNodeStateProperty failedState)
        {
            return (failedState.Exception, failedState.Explanation);
        }

        if (stateProperty is TimeoutTestNodeStateProperty timeoutState)
        {
            return (timeoutState.Exception, timeoutState.Explanation);
        }

        return (null, null);
    }

    private static int EstimateCount(TestContext testContext, TestNodeStateProperty stateProperty, bool isTrxEnabled)
    {
        var isFinalState = stateProperty is not DiscoveredTestNodeStateProperty and not InProgressTestNodeStateProperty;

        var testDetails = testContext.Metadata.TestDetails ?? throw new ArgumentNullException(nameof(testContext.Metadata.TestDetails));

        var count = 3; // State + FileLocation + MethodIdentifier

        count += testDetails.CustomProperties.Count;
        count += testDetails.Categories.Count;
        count += testDetails.Tags.Count;

        if (isFinalState)
        {
            count += 3; // Timing + StdOut + StdErr;
        }

        if (isTrxEnabled)
        {
            count += 2; // TRX TypeName + TRX Messages

            if (testDetails.Categories.Count > 0)
            {
                count += 1; // TRX Categories
            }

            if (stateProperty is ErrorTestNodeStateProperty or FailedTestNodeStateProperty or TimeoutTestNodeStateProperty)
            {
                count += 1; // Trx Exception
            }
        }

        return count;
    }

    private static bool IsTrxEnabled(TestContext testContext)
    {
        if (_cachedIsTrxEnabled.HasValue)
        {
            return _cachedIsTrxEnabled.Value;
        }

        if (testContext.Services.GetService<ITestFrameworkCapabilities>() is not {} capabilities)
        {
            _cachedIsTrxEnabled = false;
            return false;
        }

        if (capabilities.GetCapability<ITrxReportCapability>() is not TrxReportCapability trxCapability)
        {
            _cachedIsTrxEnabled = false;
            return false;
        }

        _cachedIsTrxEnabled = trxCapability.IsTrxEnabled;
        return _cachedIsTrxEnabled.Value;
    }

    private static TimingProperty GetTimingProperty(TestContext testContext, DateTimeOffset overallStart)
    {
        if (overallStart == default(DateTimeOffset))
        {
            return new TimingProperty(new TimingInfo());
        }

        var end = testContext.Execution.TestEnd ?? DateTimeOffset.Now;
        var timings = testContext.Timings;
        var stepTimings = new StepTimingInfo[timings.Count];
        var i = 0;
        foreach (var timing in timings)
        {
            stepTimings[i++] = new StepTimingInfo(timing.StepName, timing.StepName, new TimingInfo(timing.Start, timing.End, timing.Duration));
        }

        return new TimingProperty(new TimingInfo(overallStart, end, end - overallStart), stepTimings);
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

    private static string[] CreateParameterTypeArray(ParameterMetadata[] parameters)
    {
        if (parameters.Length == 0)
        {
            return [];
        }

        var array = new string[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            array[i] = parameters[i].Type.FullName!;
        }
        return array;
    }
}
