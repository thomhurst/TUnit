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
    public async Task ExcludeTests_RecordsRemovedTests_AcrossCalls()
    {
        var (context, tests) = CreateFilteringContext("A", "B", "C");

        context.ExcludeTests(t => t.Metadata.TestDetails.TestName == "A");
        context.ExcludeTests(t => t.Metadata.TestDetails.TestName == "C");

        await Assert.That(context.TestSet.Removed).IsEquivalentTo([tests[0], tests[2]], CollectionOrdering.Matching);
    }

    [Test]
    public async Task AddTests_RecordsDroppedTests_AndForgetsReAddedOnes()
    {
        var provider = CreateProvider();
        var a = CreateTestContext(provider, "A");
        var b = CreateTestContext(provider, "B");
        var discovery = provider.TestDiscoveryContext;
        discovery.TestSet.Initialize([a, b]);

        discovery.ExcludeTests(t => t.Metadata.TestDetails.TestName == "A");
        discovery.AddTests([a]);

        // A was excluded and then put back; B was dropped by AddTests.
        await Assert.That(discovery.TestSet.Removed).IsEquivalentTo([b], CollectionOrdering.Matching);
    }

    [Test]
    public async Task ExcludeTests_WithNoMatch_DoesNotMarkModified()
    {
        var (context, tests) = CreateFilteringContext("A", "B");

        context.ExcludeTests(_ => false);

        await Assert.That(context.AllTests).IsEquivalentTo(tests, CollectionOrdering.Matching);
        await Assert.That(context.TestSet.IsModified).IsFalse();
        await Assert.That(context.TestSet.Removed).IsEmpty();
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
        context.TestSet.Seal();

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
        context.TestSet.Seal();

        await Assert.That(tests[0].SkipReason).IsEqualTo("first");
    }

    [Test]
    public async Task SkipTests_KeepsAReasonSetBeforeTheStage()
    {
        var (context, tests) = CreateFilteringContext("A");
        tests[0].SkipReason = "[Skip]";

        context.SkipTests(_ => true, "hook");
        context.TestSet.Seal();

        await Assert.That(tests[0].SkipReason).IsEqualTo("[Skip]");
    }

    [Test]
    public async Task SkipTests_IsAppliedOnlyWhenTheStageEnds()
    {
        var (context, tests) = CreateFilteringContext("A");

        context.SkipTests(_ => true, "later");

        await Assert.That(tests[0].SkipReason).IsNull();

        context.TestSet.Seal();

        await Assert.That(tests[0].SkipReason).IsEqualTo("later");
    }

    [Test]
    public async Task SkipTests_ThenExclude_LeavesTheExcludedTestUntouched()
    {
        var (context, tests) = CreateFilteringContext("A", "B");
        var classInstance = tests[0].Metadata.TestDetails.ClassInstance;

        context.SkipTests(_ => true, "skipped");
        context.ExcludeTests(t => t.Metadata.TestDetails.TestName == "A");
        context.TestSet.Seal();

        await Assert.That(tests[0].SkipReason).IsNull();
        await Assert.That(tests[0].Metadata.TestDetails.ClassInstance).IsSameReferenceAs(classInstance);
        await Assert.That(tests[1].SkipReason).IsEqualTo("skipped");
    }

    [Test]
    public async Task SkipTests_WithThrowingPredicate_SkipsNothing()
    {
        var (context, tests) = CreateFilteringContext("A", "B");

        // Matches A, then throws on B: nothing may be recorded for A.
        await Assert.That(() => context.SkipTests(MatchesAThenThrows, "half")).Throws<InvalidOperationException>();
        context.TestSet.Seal();

        await Assert.That(tests.Select(t => t.SkipReason)).IsEquivalentTo([(string?)null, null], CollectionOrdering.Matching);
    }

    [Test]
    public async Task ExcludeTests_WithThrowingPredicate_ChangesNothing()
    {
        var (context, tests) = CreateFilteringContext("A", "B");

        await Assert.That(() => context.ExcludeTests(MatchesAThenThrows)).Throws<InvalidOperationException>();

        await Assert.That(context.AllTests).IsEquivalentTo(tests, CollectionOrdering.Matching);
        await Assert.That(context.TestSet.IsModified).IsFalse();
        await Assert.That(context.TestSet.Removed).IsEmpty();
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
        context.TestSet.Seal();

        // A1 -> 1; B1, B2 -> 2; C1 -> 3. Equal ranks let the scheduler keep its default order.
        await Assert.That(tests.Select(t => t.HookRank)).IsEquivalentTo([(int?)2, 1, 2, 3], CollectionOrdering.Matching);
    }

    [Test]
    public async Task OrderTestsBy_LastCallWins()
    {
        var (context, tests) = CreateFilteringContext("A", "B");

        context.OrderTestsBy(t => t.Metadata.TestDetails.TestName);
        context.OrderTestsBy(_ => 0);
        context.TestSet.Seal();

        await Assert.That(tests.Select(t => t.HookRank)).IsEquivalentTo([(int?)1, 1], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Tests_AreUnranked_UntilTheStageEnds()
    {
        var (context, tests) = CreateFilteringContext("A", "B");

        await Assert.That(tests.Select(t => t.HookRank)).IsEquivalentTo([(int?)null, null], CollectionOrdering.Matching);

        context.OrderTestsBy(t => t.Metadata.TestDetails.TestName);

        await Assert.That(tests.Select(t => t.HookRank)).IsEquivalentTo([(int?)null, null], CollectionOrdering.Matching);
    }

    [Test]
    public async Task OrderTestsBy_ThenExclude_LeavesTheExcludedTestUnranked()
    {
        var (context, tests) = CreateFilteringContext("A", "B");

        context.OrderTestsBy(t => t.Metadata.TestDetails.TestName);
        context.ExcludeTests(t => t.Metadata.TestDetails.TestName == "A");
        context.TestSet.Seal();

        await Assert.That(tests[0].HookRank).IsNull();
        await Assert.That(tests[1].HookRank).IsEqualTo(2);
    }

    [Test]
    public async Task OrderTestsBy_WithThrowingKeySelector_ChangesNothing()
    {
        var (context, tests) = CreateFilteringContext("B", "A");

        await Assert.That(() => context.OrderTestsBy<string>(
            t => t.Metadata.TestDetails.TestName == "A" ? throw new InvalidOperationException("boom") : t.Metadata.TestDetails.TestName))
            .Throws<InvalidOperationException>();
        context.TestSet.Seal();

        await Assert.That(context.AllTests).IsEquivalentTo(tests, CollectionOrdering.Matching);
        await Assert.That(context.TestSet.WasReordered).IsFalse();
        await Assert.That(tests.Select(t => t.HookRank)).IsEquivalentTo([(int?)null, null], CollectionOrdering.Matching);
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

    // Matches test "A" and throws for any other test, so a call fails after a predicate already matched.
    private static bool MatchesAThenThrows(TestContext test)
        => test.Metadata.TestDetails.TestName == "A" ? true : throw new InvalidOperationException("boom");

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
