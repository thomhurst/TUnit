using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.Building;
using TUnit.Engine.Services;

namespace TUnit.Engine;

/// <summary>
/// Unified test discovery service that uses the new pipeline architecture
/// </summary>
public sealed class TestDiscoveryServiceV2 : ITestDiscoverer, IDataProducer
{
    private readonly UnifiedTestBuilderPipeline _testBuilderPipeline;
    private readonly bool _enableDynamicDiscovery;

    public string Uid => "TUnit";
    public string Version => "2.0.0";
    public string DisplayName => "TUnit Test Discovery";
    public string Description => "TUnit Unified Test Discovery Service";
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public TestDiscoveryServiceV2(
        UnifiedTestBuilderPipeline testBuilderPipeline,
        bool enableDynamicDiscovery = false)
    {
        _testBuilderPipeline = testBuilderPipeline ?? throw new ArgumentNullException(nameof(testBuilderPipeline));
        _enableDynamicDiscovery = enableDynamicDiscovery;
    }

    /// <summary>
    /// Discovers all tests using the unified pipeline
    /// </summary>
    public async Task<IEnumerable<ExecutableTest>> DiscoverTests()
    {
        const int DiscoveryTimeoutSeconds = 30;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(DiscoveryTimeoutSeconds));

        try
        {
            return await DiscoverTestsWithTimeout(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(
                $"Test discovery timed out after {DiscoveryTimeoutSeconds} seconds. " +
                "This may indicate an issue with data sources or excessive test generation.");
        }
    }

    private async Task<IEnumerable<ExecutableTest>> DiscoverTestsWithTimeout(CancellationToken cancellationToken)
    {
        // Use the pipeline to build all tests
        var allTests = new List<ExecutableTest>();

        await foreach (var test in BuildTestsAsync(cancellationToken))
        {
            allTests.Add(test);
        }

        // No longer using TestRegistry - tests are managed directly

        // Resolve dependencies between tests
        ResolveDependencies(allTests);

        return allTests;
    }

    private async IAsyncEnumerable<ExecutableTest> BuildTestsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var executableTests = await _testBuilderPipeline.BuildTestsAsync();

        foreach (var test in executableTests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return test;
        }
    }

    private void ResolveDependencies(List<ExecutableTest> allTests)
    {
        var testMap = allTests.ToDictionary(t => t.TestId);

        foreach (var test in allTests)
        {
            var dependencies = new List<ExecutableTest>();

            // Handle new TestDependency model
            foreach (var dependency in test.Metadata.Dependencies)
            {
                var matchingTests = allTests.Where(t => dependency.Matches(t.Metadata, test.Metadata)).ToList();

                if (matchingTests.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Test '{test.DisplayName}' depends on {dependency} which was not found.");
                }

                dependencies.AddRange(matchingTests);
            }

            // Handle legacy string-based dependencies for backward compatibility
#pragma warning disable CS0618 // Type or member is obsolete
            foreach (var dependencyName in test.Metadata.DependsOn)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                // Try exact match first
                if (testMap.TryGetValue(dependencyName, out var dependency))
                {
                    dependencies.Add(dependency);
                    continue;
                }

                // Try matching by test name in same class
                var matchingTests = allTests.Where(t =>
                    t.Metadata.TestClassType == test.Metadata.TestClassType &&
                    (t.Metadata.TestName == dependencyName || t.Metadata.TestMethodName == dependencyName)).ToList();

                if (matchingTests.Count == 1)
                {
                    dependencies.Add(matchingTests[0]);
                }
                else if (matchingTests.Count > 1)
                {
                    throw new InvalidOperationException(
                        $"Test '{test.DisplayName}' depends on '{dependencyName}' which matches multiple tests. " +
                        "Use a more specific test identifier.");
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Test '{test.DisplayName}' depends on '{dependencyName}' which was not found.");
                }
            }

            // Remove duplicates and ensure we don't depend on ourselves
            test.Dependencies = dependencies
                .Distinct()
                .Where(d => d.TestId != test.TestId)
                .ToArray();
        }
    }

    /// <summary>
    /// ITestDiscoverer implementation for Microsoft.Testing.Platform
    /// </summary>
    public async Task<IEnumerable<TestNode>> DiscoverTestsAsync(
        DiscoverTestExecutionRequest discoverTestExecutionRequest,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        var tests = await DiscoverTests();
        var testNodes = new List<TestNode>();

        foreach (var test in tests)
        {
            var testNode = CreateTestNode(test);
            var message = new TestNodeUpdateMessage(
                discoverTestExecutionRequest.Session.SessionUid,
                testNode);

            await messageBus.PublishAsync(this, message);
            testNodes.Add(testNode);
        }

        return testNodes;
    }

    private static TestNode CreateTestNode(ExecutableTest test)
    {
        var propertyList = new List<IProperty>();

        // Add file location if available
        if (test.Metadata.FilePath != null && test.Metadata.LineNumber.HasValue)
        {
            propertyList.Add(new TestFileLocationProperty(
                test.Metadata.FilePath,
                new LinePositionSpan(
                    new LinePosition(test.Metadata.LineNumber.Value, 0),
                    new LinePosition(test.Metadata.LineNumber.Value, 0)
                )));
        }

        // Add test method identifier
        propertyList.Add(new TestMethodIdentifierProperty(
            Namespace: test.Metadata.TestClassType.Namespace ?? "GlobalNamespace",
            AssemblyFullName: test.Metadata.TestClassType.Assembly.FullName ?? "UnknownAssembly",
            TypeName: test.Metadata.TestClassType.FullName ?? test.Metadata.TestClassType.Name,
            MethodName: test.Metadata.TestMethodName,
            ParameterTypeFullNames: test.Metadata.ParameterTypes?.Select(t => t.FullName ?? "unknown").ToArray() ?? [],
            ReturnTypeFullName: "void",
            MethodArity: 0
        ));

        // Add categories as metadata
        foreach (var category in test.Metadata.Categories)
        {
            propertyList.Add(new TestMetadataProperty(category));
        }

        // Add TRX properties
        propertyList.Add(new TrxFullyQualifiedTypeNameProperty(
            test.Metadata.TestClassType.FullName ?? test.Metadata.TestClassType.Name));
        propertyList.Add(new TrxCategoriesProperty(test.Metadata.Categories.ToArray()));

        // Add test state if already failed (e.g., from data source expansion error)
        if (test.State == TestState.Failed && test.Result?.Exception != null)
        {
            propertyList.Add(new FailedTestNodeStateProperty(test.Result.Exception));
        }

        return new TestNode
        {
            Uid = new TestNodeUid(test.TestId),
            DisplayName = test.DisplayName,
            Properties = new PropertyBag(propertyList)
        };
    }
}
