using TUnit.Core;
using TUnit.Engine;
using TUnit.Engine.Services;

namespace TUnit.UnitTests;

public class TestDependencyResolverTests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Completed_Empty_Placeholder_Preserves_Outcome_And_Remains_A_Dependency(bool failed)
    {
        var template = TestRunnerTests.CreateTest("empty", dependencyMetadata: [TestDependency.FromMethodName("missing")]);
        var placeholder = new DeferredEnumerationExecutableTest
        {
            TestId = template.TestId,
            Metadata = template.Metadata,
            Arguments = [],
            Context = template.Context
        };
        var resolver = new TestDependencyResolver();
        resolver.RegisterTest(placeholder);
        // Queue an unresolved dependency before expansion completes without children.
        await Assert.That(resolver.TryResolveDependencies(placeholder)).IsFalse();
        var state = failed ? TestState.Failed : TestState.Passed;
        placeholder.CompleteExpansionWithoutChildren(failed ? new InvalidOperationException("expansion failed") : null);
        await Assert.That(placeholder.Context.Result).IsNotNull();
        await Assert.That(placeholder.ExpansionCompletedWithoutChildren).IsTrue();
        var result = placeholder.Result;
        var other = TestRunnerTests.CreateTest("other", dependencies: [placeholder]);
        placeholder.Dependencies = [new ResolvedDependency
        {
            Test = other,
            Metadata = TestDependency.FromMethodName("other")
        }];
        resolver.RegisterTest(other);
        var dependent = TestRunnerTests.CreateTest("dependent", dependencyMetadata: [TestDependency.FromMethodName("empty")]);
        resolver.RegisterTest(dependent);

        resolver.ReplaceDeferredTests([(placeholder, Array.Empty<AbstractExecutableTest>())]);
        resolver.ResolveAllDependencies();

        await Assert.That(placeholder.Dependencies.Length).IsEqualTo(0);
        await Assert.That(new CircularDependencyDetector().DetectCircularDependencies([other]).Count).IsEqualTo(0);
        await Assert.That(placeholder.State).IsEqualTo(state);
        await Assert.That(ReferenceEquals(placeholder.Result, result)).IsTrue();
        await Assert.That(dependent.Dependencies.Length).IsEqualTo(1);
        await Assert.That(ReferenceEquals(dependent.Dependencies[0].Test, placeholder)).IsTrue();
    }
}
