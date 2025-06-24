using System.Threading.Tasks;
using TUnit.Core.Interfaces;

namespace TUnit.Core;


/// <summary>
/// Specifies that a test method, class, or assembly belongs to a parallel execution group.
/// </summary>
/// <remarks>
/// <para>
/// Tests within the same parallel group will run in parallel with each other but not with tests from other groups.
/// This attribute helps to organize test execution when you want to control how certain tests run in relation to others.
/// </para>
/// <para>
/// The test engine processes parallel groups sequentially based on their <see cref="Order"/> property, but tests
/// within the same group execute in parallel with each other. This is useful for organizing tests that should
/// run concurrently but in separate batches from other test groups.
/// </para>
/// <para>
/// This attribute implements <see cref="ITestDiscoveryEventReceiver"/> to register a <see cref="ParallelGroupConstraint"/>
/// with the test discovery system, which the test execution engine uses to organize and schedule test execution.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// [Test, ParallelGroup("DatabaseTests")]
/// public async Task DatabaseTest1()
/// {
///     // This test will run in parallel with other tests in the "DatabaseTests" group
/// }
/// 
/// [Test, ParallelGroup("DatabaseTests")]
/// public async Task DatabaseTest2()
/// {
///     // This will run in parallel with DatabaseTest1
/// }
/// 
/// [Test, ParallelGroup("UITests", Order = 1)]
/// public async Task UITest1()
/// {
///     // This test belongs to a different
/// }
/// </code>
/// </para>
/// </remarks>
public class ParallelGroupAttribute(string group) : TUnitAttribute, ITestDiscoveryEventReceiver
{
    /// <inheritdoc />
    public int Order { get; set; }

    /// <summary>
    /// Gets the name of the parallel group to which the test belongs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The group name is used to identify which tests should be executed in parallel with each other.
    /// Tests decorated with <see cref="ParallelGroupAttribute"/> that have the same <see cref="Group"/> value
    /// will be grouped together and executed concurrently, but isolated from tests in other groups.
    /// </para>
    /// <para>
    /// Group names are case-sensitive string identifiers that should be chosen to represent a logical
    /// set of tests that can safely run in parallel with each other. For example, "DatabaseTests",
    /// "NetworkTests", or "UITests".
    /// </para>
    /// <para>
    /// During test execution, the test engine organizes tests into groups based on this property, and 
    /// ensures that tests from different groups are not executed simultaneously, helping to manage
    /// resource contention and test isolation.
    /// </para>
    /// <para>
    /// This property is set via the constructor parameter and cannot be changed after the attribute
    /// is instantiated.
    /// </para>
    /// </remarks>
    /// <seealso cref="ParallelGroupConstraint"/>
    /// <seealso cref="IParallelConstraint"/>
    /// <seealso cref="NotInParallelAttribute"/>
    public string Group { get; } = group;

    /// <inheritdoc />
    public ValueTask OnTestDiscovered(TestContext testContext)
    {
        testContext.SetParallelConstraint(new ParallelGroupConstraint(Group, Order));
        return default;
    }
}