using TUnit.Assertions.Enums;

namespace TUnit.UnitTests;

/// <summary>
/// Covers the exclude/skip/order operations that <see cref="TestDiscoveryContext"/> and
/// <see cref="TestFilteringContext"/> expose to hooks (https://github.com/thomhurst/TUnit/issues/4788).
/// </summary>
public class MutableTestSetTests
{
    [Test]
    public async Task ExcludeTests_RemovesMatchingTests_AndKeepsOrder()
    {
        var (context, tests) = CreateFilteringContext("A", "B", "C");

        context.ExcludeTests(t => t.Metadata.TestDetails.TestName == "B");

        await Assert.That(context.AllTests).IsEquivalentTo([tests[0], tests[2]], CollectionOrdering.Matching);
        await Assert.That(context.TestSet.IsModified).IsTrue();
    }

    [Test]
    public async Task ExcludeTests_WithNoMatch_DoesNotMarkModified()
    {
        var (context, tests) = CreateFilteringContext("A", "B");

        context.ExcludeTests(_ => false);

        await Assert.That(context.AllTests).IsEquivalentTo(tests, CollectionOrdering.Matching);
        await Assert.That(context.TestSet.IsModified).IsFalse();
    }

    [Test]
    public async Task ExcludeTests_InvalidatesTestClasses()
    {
        var (context, _) = CreateFilteringContext("A");
        await Assert.That(context.TestClasses).Count().IsEqualTo(1);

        context.ExcludeTests(_ => true);

        await Assert.That(context.TestClasses).IsEmpty();
        await Assert.That(context.Assemblies).IsEmpty();
    }

    [Test]
    public async Task SkipTests_SetsReason_AndKeepsTestsInTheSet()
    {
        var (context, tests) = CreateFilteringContext("A", "B");

        context.SkipTests(t => t.Metadata.TestDetails.TestName == "A", "not tonight");

        await Assert.That(context.AllTests).Count().IsEqualTo(2);
        await Assert.That(tests[0].SkipReason).IsEqualTo("not tonight");
        await Assert.That(tests[1].SkipReason).IsNull();
        await Assert.That(context.TestSet.IsModified).IsFalse();
    }

    [Test]
    public async Task SkipTests_KeepsTheFirstReason()
    {
        var (context, tests) = CreateFilteringContext("A");

        context.SkipTests(_ => true, "first");
        context.SkipTests(_ => true, "second");

        await Assert.That(tests[0].SkipReason).IsEqualTo("first");
    }

    [Test]
    public async Task SkipTests_RequiresReason()
    {
        var (context, _) = CreateFilteringContext("A");

        await Assert.That(() => context.SkipTests(_ => true, "")).Throws<ArgumentException>();
    }

    [Test]
    public async Task OrderTestsBy_IsStable_AndHonoursComparer()
    {
        var (context, tests) = CreateFilteringContext("B1", "A1", "B2", "A2");

        // Order by first letter, descending: equal keys keep their original relative order.
        context.OrderTestsBy(t => t.Metadata.TestDetails.TestName[0], Comparer<char>.Create((x, y) => y.CompareTo(x)));

        await Assert.That(context.AllTests).IsEquivalentTo([tests[0], tests[2], tests[1], tests[3]], CollectionOrdering.Matching);
        await Assert.That(context.TestSet.WasReordered).IsTrue();
        await Assert.That(context.TestSet.IsModified).IsFalse();
    }

    [Test]
    public async Task OrderTestsBy_GivesEqualKeysTheSameRank()
    {
        var (context, tests) = CreateFilteringContext("B1", "A1", "B2", "C1");

        context.OrderTestsBy(t => t.Metadata.TestDetails.TestName[0]);

        // A1 -> 1; B1, B2 -> 2; C1 -> 3. Equal ranks let the scheduler keep its default order.
        await Assert.That(tests.Select(t => t.HookRank)).IsEquivalentTo([2, 1, 2, 3], CollectionOrdering.Matching);
    }

    [Test]
    public async Task OrderTestsBy_LastCallWins()
    {
        var (context, tests) = CreateFilteringContext("A", "B");

        context.OrderTestsBy(t => t.Metadata.TestDetails.TestName);
        context.OrderTestsBy(_ => 0);

        await Assert.That(tests.Select(t => t.HookRank)).IsEquivalentTo([1, 1], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Tests_AreUnranked_UntilOrdered()
    {
        var (_, tests) = CreateFilteringContext("A", "B");

        await Assert.That(tests.Select(t => t.HookRank)).IsEquivalentTo([0, 0], CollectionOrdering.Matching);
    }

    [Test]
    public async Task SealedSet_RejectsChanges()
    {
        var (context, _) = CreateFilteringContext("A");
        context.TestSet.Seal();

        await Assert.That(() => context.ExcludeTests(_ => true)).Throws<InvalidOperationException>();
        await Assert.That(() => context.SkipTests(_ => true, "reason")).Throws<InvalidOperationException>();
        await Assert.That(() => context.OrderTestsBy(t => t.Metadata.TestDetails.TestName)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Initialize_ResetsFlagsAndSeal()
    {
        var (context, tests) = CreateFilteringContext("A", "B");
        context.ExcludeTests(_ => true);
        context.TestSet.Seal();

        context.TestSet.Initialize(tests);

        await Assert.That(context.TestSet.IsSealed).IsFalse();
        await Assert.That(context.TestSet.IsModified).IsFalse();
        await Assert.That(context.AllTests).Count().IsEqualTo(2);
    }

    [Test]
    public async Task DiscoveryContext_AddTests_ThrowsOnceSealed()
    {
        var provider = CreateProvider();
        var discovery = provider.TestDiscoveryContext;
        var test = CreateTestContext(provider, "A");

        discovery.AddTests([test]);
        await Assert.That(discovery.AllTests).Count().IsEqualTo(1);

        discovery.TestSet.Seal();

        await Assert.That(() => discovery.AddTests([])).Throws<InvalidOperationException>();
    }

    private static (TestFilteringContext Context, TestContext[] Tests) CreateFilteringContext(params string[] names)
    {
        var provider = CreateProvider();
        var tests = names.Select(name => CreateTestContext(provider, name)).ToArray();
        var context = provider.TestFilteringContext;
        context.TestSet.Initialize(tests);
        return (context, tests);
    }

    private static ContextProvider CreateProvider()
        => new(new EmptyServiceProvider(), Guid.NewGuid().ToString(), testFilter: null);

    private static TestContext CreateTestContext(ContextProvider provider, string testName)
    {
        var classMetadata = new ClassMetadata
        {
            Type = typeof(DummyTestClass),
            TypeInfo = new ConcreteType(typeof(DummyTestClass)),
            Name = nameof(DummyTestClass),
            Namespace = typeof(DummyTestClass).Namespace ?? string.Empty,
            Assembly = new AssemblyMetadata
            {
                Name = typeof(DummyTestClass).Assembly.GetName().Name ?? string.Empty
            },
            Parent = null,
            Parameters = [],
            Properties = []
        };

        var methodMetadata = MethodMetadataFactory.Create(
            nameof(DummyTestClass.SomeTest),
            typeof(DummyTestClass),
            typeof(Task),
            classMetadata);

        var testDetails = new TestDetails([])
        {
            TestId = testName,
            TestName = testName,
            ClassType = typeof(DummyTestClass),
            MethodName = nameof(DummyTestClass.SomeTest),
            ClassInstance = PlaceholderInstance.Instance,
            TestMethodArguments = [],
            TestClassArguments = [],
            MethodMetadata = methodMetadata,
            ReturnType = typeof(Task),
            AttributesByType = new Dictionary<Type, IReadOnlyList<Attribute>>()
        };

        return provider.CreateTestContext(
            typeof(DummyTestClass),
            new TestBuilderContext { TestMetadata = methodMetadata },
            testDetails,
            CancellationToken.None);
    }

    private sealed class DummyTestClass
    {
        public Task SomeTest() => Task.CompletedTask;
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
