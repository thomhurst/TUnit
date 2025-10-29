using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Test dependency information and relationships
/// Implements <see cref="ITestDependencies"/> interface
/// </summary>
public partial class TestContext
{
    // Explicit interface implementations for ITestDependencies
    IReadOnlyList<TestDetails> ITestDependencies.TestDependencies => TestDependencies;
    string? ITestDependencies.ParentTestId => ParentTestId;
    TestRelationship ITestDependencies.Relationship => Relationship;

    IEnumerable<TestContext> ITestDependencies.GetTests(Func<TestContext, bool> predicate) => GetTests(predicate);
    List<TestContext> ITestDependencies.GetTests(string testName) => GetTests(testName);
    List<TestContext> ITestDependencies.GetTests(string testName, Type classType) => GetTests(testName, classType);

    // Rename backing field to avoid conflict
    public IReadOnlyList<TestDetails> TestDependencies => _dependencies;
    internal readonly List<TestDetails> _dependencies = [];
}
