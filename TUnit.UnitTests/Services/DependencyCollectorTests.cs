using NSubstitute;
using TUnit.Core.Exceptions;
using TUnit.Core.Helpers;
using TUnit.Engine.Services;
using TUnit.UnitTests.Extensions;

namespace TUnit.UnitTests.Services;

public class DependencyCollectorTests
{
    [Test]
    public void CollectDependencies_ShouldThrowDependencyConflictException_WhenCircularDependencyExists()
    {
        // Arrange
        var testA = CreateTest("TestA");
        var testB = CreateTest("TestB", "TestA");

        typeof(TestDetails).GetProperty(nameof(TestDetails.Attributes))!
            .GetBackingField()!
            .SetValue(testA.TestDetails, new Attribute[] { new DependsOnAttribute(testB.TestDetails.TestName) });

        var collector = new DependencyCollector();
        var visited = new HashSet<TestDetails>([testA.TestDetails], new DependencyCollector.TestDetailsEqualityComparer());
        var currentChain = new HashSet<TestDetails>([testA.TestDetails], new DependencyCollector.TestDetailsEqualityComparer());
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        Assert.Throws<DependencyConflictException>(() =>
        {
            _ = collector.CollectDependencies(testA, [testA, testB], visited, currentChain, cancellationToken).ToArray();
        });
    }

    [Test]
    public async Task CollectDependencies_ShouldResolveDependenciesCorrectly_WhenNoConflictsExist()
    {
        // Arrange
        var testA = CreateTest("TestA");
        var testB = CreateTest("TestB", "TestA");

        var collector = new DependencyCollector();
        var visited = new HashSet<TestDetails>([testB.TestDetails], new DependencyCollector.TestDetailsEqualityComparer());
        var currentChain = new HashSet<TestDetails>([testB.TestDetails], new DependencyCollector.TestDetailsEqualityComparer());
        var cancellationToken = CancellationToken.None;

        // Act
        var dependencies = collector.CollectDependencies(testB, [testA, testB], visited, currentChain, cancellationToken).ToList();

        // Assert
        await Assert.That(dependencies).HasSingleItem();
        await Assert.That(dependencies[0].Test).IsEqualTo(testA);
    }

    [Test]
    public async Task CollectDependencies_ShouldHandleNestedDependenciesCorrectly()
    {
        // Arrange
        var testA = CreateTest("TestA");
        var testB = CreateTest("TestB", "TestA");
        var testC = CreateTest("TestC", "TestB");

        var collector = new DependencyCollector();
        var visited = new HashSet<TestDetails>([testC.TestDetails], new DependencyCollector.TestDetailsEqualityComparer());
        var currentChain = new HashSet<TestDetails>([testC.TestDetails], new DependencyCollector.TestDetailsEqualityComparer());
        var cancellationToken = CancellationToken.None;

        // Act
        var dependencies = collector.CollectDependencies(testC, [testA, testB, testC], visited, currentChain, cancellationToken).ToList();

        // Assert
        await Assert.That(dependencies).HasCount().EqualTo(2);
        await Assert.That(dependencies).Contains(d => d.Test == testA);
        await Assert.That(dependencies).Contains(d => d.Test == testB);
    }

    [Test]
public async Task CollectDependencies_ShouldResolveComplexNestedDependenciesCorrectly_WhenNoConflictsExist()
{
    // Arrange
    var testA = CreateTest("TestA");
    var testB = CreateTest("TestB", "TestA");
    var testC = CreateTest("TestC", "TestB");
    var testD = CreateTest("TestD", "TestC");

    var collector = new DependencyCollector();
    var visited = new HashSet<TestDetails>([testD.TestDetails], new DependencyCollector.TestDetailsEqualityComparer());
    var currentChain = new HashSet<TestDetails>([testD.TestDetails], new DependencyCollector.TestDetailsEqualityComparer());
    var cancellationToken = CancellationToken.None;

    // Act
    var dependencies = collector.CollectDependencies(testD, [testA, testB, testC, testD], visited, currentChain, cancellationToken).ToList();

    // Assert
    await Assert.That(dependencies).HasCount().EqualTo(3);
    await Assert.That(dependencies).Contains(d => d.Test == testA);
    await Assert.That(dependencies).Contains(d => d.Test == testB);
    await Assert.That(dependencies).Contains(d => d.Test == testC);
}

[Test]
public void CollectDependencies_ShouldThrowDependencyConflictException_ForComplexNestedGraphWithConflict()
{
    // Arrange
    var testA = CreateTest("TestA");
    var testB = CreateTest("TestB", "TestA");
    var testC = CreateTest("TestC", "TestB");
    var testD = CreateTest("TestD", "TestC");

    typeof(TestDetails).GetProperty(nameof(TestDetails.Attributes))!
        .GetBackingField()!
        .SetValue(testA.TestDetails, new Attribute[] { new DependsOnAttribute(testD.TestDetails.TestName) });

    var collector = new DependencyCollector();
    var visited = new HashSet<TestDetails>([testD.TestDetails], new DependencyCollector.TestDetailsEqualityComparer());
    var currentChain = new HashSet<TestDetails>([testD.TestDetails], new DependencyCollector.TestDetailsEqualityComparer());
    var cancellationToken = CancellationToken.None;

    // Act & Assert
    Assert.Throws<DependencyConflictException>(() =>
    {
        _ = collector.CollectDependencies(testD, [testA, testB, testC, testD], visited, currentChain, cancellationToken).ToArray();
    });
}

    private DiscoveredTest CreateTest(string name, string? dependsOn = null)
    {
        var resettableLazy = new ResettableLazy<DependencyCollectorTests>(() => new DependencyCollectorTests(), string.Empty, new TestBuilderContext());

        var testDetails = new TestDetails<DependencyCollectorTests>
        {
            TestName = name,
            TestId = Guid.NewGuid()
                .ToString("N"),
            TestMethodArguments = [],
            TestClassArguments = [],
            TestClassInjectedPropertyArguments = new Dictionary<string, object?>(),
            MethodMetadata = new MethodMetadata
            {
                Attributes = dependsOn != null
                    ?
                    [
                        new DependsOnAttribute(dependsOn)
                    ]
                    : [],
                Class = new ClassMetadata
                {
                    Parent = ReflectionToSourceModelHelpers.GetParent(typeof(DependencyCollectorTests)),
                    Type = typeof(DependencyCollectorTests),
                    Namespace = null,
                    Assembly = new AssemblyMetadata
                    {
                        Name = typeof(DependencyCollectorTests).Assembly.GetName().Name!,
                        Attributes =
                        [
                        ]
                    },
                    Parameters = [],
                    Properties = [],
                    Constructors = [],
                    Name = nameof(DependencyCollectorTests),
                    Attributes = []
                },
                Parameters =
                [
                ],
                GenericTypeCount = 0,
                ReturnType = typeof(Task),
                Type = typeof(DependencyCollectorTests),
                Name = name,
            },
            CurrentRepeatAttempt = 0,
            RepeatLimit = 0,
            DataAttributes = [],
            ReturnType = typeof(Task),
            TestFilePath = string.Empty,
            TestLineNumber = 0,
            LazyClassInstance = resettableLazy,
        };

        var testDiscoveryContext = new BeforeTestDiscoveryContext()
        {
            TestFilter = ""
        };
        var beforeTestDiscoveryContext = new TestDiscoveryContext(testDiscoveryContext)
        {
            TestFilter = ""
        };
        var testSessionContext = new TestSessionContext(beforeTestDiscoveryContext)
        {
            TestFilter = "",
            Id = "test-session-id",
        };
        var assemblyHookContext = new AssemblyHookContext(testSessionContext)
        {
            Assembly = typeof(DependencyCollectorTests).Assembly
        };
        var classContext = new ClassHookContext(assemblyHookContext)
        {
            ClassType = typeof(DependencyCollectorTests)
        };

        return new DiscoveredTest<DependencyCollectorTests>(resettableLazy)
        {
            TestContext = new TestContext(Substitute.For<IServiceProvider>(),
                testDetails,
                new TestDefinition
                {
                    TestClassFactory = () => new object(),
                    TestMethodInvoker = (_, _) => new ValueTask(),
                    TestId = "test-id",
                    MethodMetadata = testDetails.MethodMetadata,
                    RepeatCount = 1,
                    TestFilePath = "test.cs",
                    TestLineNumber = 1,
                    ClassArgumentsProvider = () => [],
                    MethodArgumentsProvider = () => [],
                    PropertiesProvider = () => new Dictionary<string, object?>()
                },
                new TestBuilderContext(),
                classContext!),
            TestBody = (_, _) => default(ValueTask),
        };
    }
}
