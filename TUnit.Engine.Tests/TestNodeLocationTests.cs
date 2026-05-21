#pragma warning disable TPEXP

using Microsoft.Testing.Platform.Extensions.Messages;
using Shouldly;
using TUnit.Core;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.Tests;

public class TestNodeLocationTests
{
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
