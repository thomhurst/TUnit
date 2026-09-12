#pragma warning disable TPEXP

using Microsoft.Testing.Platform.Extensions.Messages;
using Shouldly;
using TUnit.Core;
using TUnit.Engine.Extensions;
using TUnit.Engine.Reporters;

namespace TUnit.Engine.Tests;

[NotInParallel]
public class TestNodeLocationTests
{
    [Test]
    public void ClearCaches_Refreshes_Metadata_For_Existing_Contexts()
    {
        var context = CreateTestContext(Guid.NewGuid().ToString("N"), "Before.cs", 1, 0, 1, 0);
        try
        {
            var before = context.ToTestNode(DiscoveredTestNodeStateProperty.CachedInstance);
            context.CachedReportingProperties.ShouldNotBeNull();
            context.Metadata.TestDetails.TestFilePath = "After.cs";
            TestExtensions.ClearCaches();
            context.CachedReportingProperties.ShouldBeNull();
            var after = context.ToTestNode(InProgressTestNodeStateProperty.CachedInstance);

            before.Properties.AsEnumerable().OfType<TestFileLocationProperty>().Single().FilePath.ShouldBe("Before.cs");
            after.Properties.AsEnumerable().OfType<TestFileLocationProperty>().Single().FilePath.ShouldBe("After.cs");
            before.Properties.AsEnumerable().OfType<DiscoveredTestNodeStateProperty>().Count().ShouldBe(1);
            after.Properties.AsEnumerable().OfType<InProgressTestNodeStateProperty>().Count().ShouldBe(1);
            context.RemoveFromRegistry();
            context.CachedReportingProperties.ShouldBeNull();
        }
        finally
        {
            context.RemoveFromRegistry();
            context.Dispose();
        }
    }

    [Test]
    [Arguments("Passed")]
    [Arguments("Failed")]
    [Arguments("Error")]
    [Arguments("Timeout")]
    [Arguments("Skipped")]
    [Arguments("Cancelled")]
    public void Final_Updates_Release_Cache_Without_Coordinator_Cleanup(string state)
    {
        var context = CreateTestContext(Guid.NewGuid().ToString("N"), "Tests.cs", 1, 0, 1, 0);
        try
        {
            context.Metadata.TestDetails.Categories.Add("Category");
            var discovered = context.ToTestNode(DiscoveredTestNodeStateProperty.CachedInstance);
            context.CachedReportingProperties.ShouldNotBeNull();

#pragma warning disable CS0618, MTP0001 // Exercise the engine's cancellation reporting path.
            TestNodeStateProperty finalState = state switch
            {
                "Passed" => PassedTestNodeStateProperty.CachedInstance,
                "Failed" => new FailedTestNodeStateProperty(new Exception("failure")),
                "Error" => new ErrorTestNodeStateProperty(new Exception("error")),
                "Timeout" => new TimeoutTestNodeStateProperty(),
                "Skipped" => new SkippedTestNodeStateProperty("skipped"),
                "Cancelled" => new CancelledTestNodeStateProperty(),
                _ => throw new ArgumentOutOfRangeException(nameof(state))
            };
#pragma warning restore CS0618, MTP0001
            var final = context.ToTestNode(finalState);

            context.CachedReportingProperties.ShouldBeNull();
            final.Properties.AsEnumerable().OfType<TestNodeStateProperty>().Single().ShouldBeSameAs(finalState);
            final.Properties.AsEnumerable().OfType<TestFileLocationProperty>().Single().FilePath.ShouldBe("Tests.cs");
            final.Properties.AsEnumerable().OfType<TestMetadataProperty>().Single()
                .ShouldBeSameAs(discovered.Properties.AsEnumerable().OfType<TestMetadataProperty>().Single());
        }
        finally
        {
            context.RemoveFromRegistry();
            context.Dispose();
        }
    }

    [Test]
    public void Concurrent_Updates_Keep_Separate_Message_State()
    {
        var context = CreateTestContext(Guid.NewGuid().ToString("N"), "Tests.cs", 1, 0, 1, 0);
        try
        {
            var nodes = new TestNode[64];
            Parallel.For(0, nodes.Length, i => nodes[i] = context.ToTestNode(i % 2 == 0
                ? DiscoveredTestNodeStateProperty.CachedInstance
                : InProgressTestNodeStateProperty.CachedInstance));

            for (var i = 0; i < nodes.Length; i++)
            {
                var state = nodes[i].Properties.AsEnumerable().OfType<TestNodeStateProperty>().Single();
                (state is DiscoveredTestNodeStateProperty).ShouldBe(i % 2 == 0);
                nodes[i].Properties.AsEnumerable().OfType<TestFileLocationProperty>().Single().FilePath.ShouldBe("Tests.cs");
            }
        }
        finally
        {
            context.RemoveFromRegistry();
            context.Dispose();
        }
    }

    [Test]
    public void ToTestNode_Uses_Source_Span_For_Mtp_File_Location()
    {
        TestExtensions.ClearCaches();

        var context = CreateTestContext(
            testId: Guid.NewGuid().ToString("N"),
            filePath: @"C:\tests\SampleTests.cs",
            lineNumber: 12,
            startColumnNumber: 5,
            endLineNumber: 16,
            endColumnNumber: 6);

        var node = context.ToTestNode(DiscoveredTestNodeStateProperty.CachedInstance);

        var location = node.Properties.AsEnumerable()
            .OfType<TestFileLocationProperty>()
            .Single();

        location.FilePath.ShouldBe(@"C:\tests\SampleTests.cs");
        location.LineSpan.Start.Line.ShouldBe(12);
        location.LineSpan.Start.Column.ShouldBe(5);
        location.LineSpan.End.Line.ShouldBe(16);
        location.LineSpan.End.Column.ShouldBe(6);
    }

    [Test]
    public void ToTestNode_Falls_Back_To_Start_Line_When_End_Line_Is_Unavailable()
    {
        TestExtensions.ClearCaches();

        var context = CreateTestContext(
            testId: Guid.NewGuid().ToString("N"),
            filePath: @"C:\tests\SampleTests.cs",
            lineNumber: 12,
            startColumnNumber: 0,
            endLineNumber: 0,
            endColumnNumber: 0);

        var node = context.ToTestNode(DiscoveredTestNodeStateProperty.CachedInstance);

        var location = node.Properties.AsEnumerable()
            .OfType<TestFileLocationProperty>()
            .Single();

        location.LineSpan.Start.Line.ShouldBe(12);
        location.LineSpan.Start.Column.ShouldBe(0);
        location.LineSpan.End.Line.ShouldBe(12);
        location.LineSpan.End.Column.ShouldBe(0);
    }

    [Test]
    public void ToTestNode_Attaches_RetryAttempts_On_Final_State_Only()
    {
        // #6119: failed retry attempts are captured on the TestContext during execution. They
        // must ride along on the final node so the HTML report can rebuild the attempt history;
        // intermediate (Discovered/InProgress) updates carry no final result, so nothing attaches.
        TestExtensions.ClearCaches();

        var context = CreateTestContext(
            testId: Guid.NewGuid().ToString("N"),
            filePath: @"C:\tests\SampleTests.cs",
            lineNumber: 12,
            startColumnNumber: 5,
            endLineNumber: 16,
            endColumnNumber: 6);

        context.RetryAttempts =
        [
            new TestResult { State = TestState.Failed, Start = null, End = null, Duration = TimeSpan.FromMilliseconds(50), Exception = new Exception("boom"), ComputerName = "test" },
        ];

        // Final state -> attached.
        var finalNode = context.ToTestNode(PassedTestNodeStateProperty.CachedInstance);
        var attached = finalNode.Properties.AsEnumerable().OfType<TUnitRetryAttemptsProperty>().SingleOrDefault();
        attached.ShouldNotBeNull();
        attached!.Attempts.Count.ShouldBe(1);
        attached.Attempts[0].State.ShouldBe(TestState.Failed);

        // Discovered/in-progress state -> not attached.
        var discoveredNode = context.ToTestNode(DiscoveredTestNodeStateProperty.CachedInstance);
        discoveredNode.Properties.AsEnumerable().OfType<TUnitRetryAttemptsProperty>().ShouldBeEmpty();
    }

    private static TestContext CreateTestContext(
        string testId,
        string filePath,
        int lineNumber,
        int startColumnNumber,
        int endLineNumber,
        int endColumnNumber)
    {
        var classMetadata = new ClassMetadata
        {
            Type = typeof(TestNodeLocationTests),
            TypeInfo = new ConcreteType(typeof(TestNodeLocationTests)),
            Name = nameof(TestNodeLocationTests),
            Namespace = typeof(TestNodeLocationTests).Namespace,
            Assembly = new AssemblyMetadata
            {
                Name = typeof(TestNodeLocationTests).Assembly.GetName().Name ?? string.Empty
            },
            Parent = null,
            Parameters = [],
            Properties = []
        };

        var methodMetadata = new MethodMetadata
        {
            Type = typeof(TestNodeLocationTests),
            TypeInfo = new ConcreteType(typeof(TestNodeLocationTests)),
            Name = nameof(ToTestNode_Uses_Source_Span_For_Mtp_File_Location),
            GenericTypeCount = 0,
            ReturnType = typeof(void),
            ReturnTypeInfo = new ConcreteType(typeof(void)),
            Parameters = [],
            Class = classMetadata
        };

        var beforeDiscoveryContext = new BeforeTestDiscoveryContext { TestFilter = null };
        var discoveryContext = new TestDiscoveryContext(beforeDiscoveryContext) { TestFilter = null };
        var sessionContext = new TestSessionContext(discoveryContext)
        {
            Id = Guid.NewGuid().ToString("N"),
            TestFilter = null
        };
        var assemblyContext = new AssemblyHookContext(sessionContext)
        {
            Assembly = typeof(TestNodeLocationTests).Assembly
        };
        var classContext = new ClassHookContext(assemblyContext)
        {
            ClassType = typeof(TestNodeLocationTests)
        };
        var builderContext = new TestBuilderContext
        {
            TestMetadata = methodMetadata
        };

        var context = new TestContext(testId, EmptyServiceProvider.Instance, classContext, builderContext, CancellationToken.None);
        context.TestDetails = new TestDetails<TestNodeLocationTests>([])
        {
            TestId = testId,
            TestName = methodMetadata.Name,
            ClassType = typeof(TestNodeLocationTests),
            MethodName = methodMetadata.Name,
            ClassInstance = new TestNodeLocationTests(),
            TestMethodArguments = [],
            TestClassArguments = [],
            MethodMetadata = methodMetadata,
            TestFilePath = filePath,
            TestLineNumber = lineNumber,
            TestStartColumnNumber = startColumnNumber,
            TestEndLineNumber = endLineNumber,
            TestEndColumnNumber = endColumnNumber,
            ReturnType = typeof(void),
            AttributesByType = new Dictionary<Type, IReadOnlyList<Attribute>>()
        };

        return context;
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new();

        public object? GetService(Type serviceType) => null;
    }
}
