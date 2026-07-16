using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Specifies that a test method, test class, or assembly should not be executed in parallel with other tests that share the same constraint keys.
/// </summary>
/// <remarks>
/// The NotInParallelAttribute helps control test execution to prevent tests from interfering with each other when they
/// access shared resources. When multiple tests share the same constraint key, they will be executed sequentially rather than in parallel.
///
/// NotInParallel can be applied at the method, class, or assembly level.
/// When applied at a class level, all test methods in the class will follow the parallel execution constraint.
/// When applied at the assembly level, it affects all tests in the assembly.
///
/// Method-level attributes take precedence over class-level attributes, which take precedence over assembly-level attributes.
///
/// Tests with no overlapping constraint keys can still run in parallel with each other.
/// To prevent a test from running in parallel with any other test, use the attribute without specifying constraint keys.
/// </remarks>
/// <example>
/// <code>
/// // Prevent this test from running in parallel with any other test with the "Database" constraint key
/// [Test]
/// [NotInParallel("Database")]
/// public void TestThatAccessesDatabase()
/// {
///     // This test will not run in parallel with any other test that has the "Database" constraint
/// }
///
/// // Prevent this test from running in parallel with tests that have either "Api" or "Database" constraint keys
/// [Test]
/// [NotInParallel(new[] { "Api", "Database" })]
/// public void TestThatAccessesMultipleResources()
/// {
///     // This test will not run in parallel with tests that have "Api" or "Database" constraints
/// }
///
/// // Prevent this test from running in parallel with any other test
/// [Test]
/// [NotInParallel]
/// public void TestThatMustRunIsolated()
/// {
///     // This test will run exclusively
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class NotInParallelAttribute : SingleTUnitAttribute, ITestDiscoveryEventReceiver, IScopedAttribute
{
    public string[] ConstraintKeys { get; } = [];

    public int Order { get; init; } = int.MaxValue / 2;

    public NotInParallelAttribute()
    {
    }

    public NotInParallelAttribute(string constraintKey) : this([constraintKey])
    {
        if (constraintKey is null or { Length: < 1 })
        {
            throw new ArgumentException("No constraint key was provided");
        }
    }

    public NotInParallelAttribute(string[] constraintKeys)
    {
        if (constraintKeys.Length != constraintKeys.Distinct().Count())
        {
            throw new ArgumentException("Duplicate constraint keys are not allowed.");
        }

        ConstraintKeys = constraintKeys;
    }

    public ValueTask OnTestDiscovered(DiscoveredTestContext context)
    {
        context.AddParallelConstraint(new NotInParallelConstraint(ConstraintKeys)
        {
            Order = Order
        });
        return default(ValueTask);
    }

    public Type ScopeType => typeof(NotInParallelAttribute);
}
