namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines a marker interface for constraints that control test parallelization behavior.
/// </summary>
/// <remarks>
/// <para>
/// This interface serves as a base for concrete constraint implementations that determine how tests are organized
/// and executed in parallel or sequentially. The test execution engine uses these constraints to group and schedule tests.
/// </para>
/// <para>
/// There are two primary implementations of this interface:
/// <list type="bullet">
/// <item><description><see cref="NotInParallelConstraint"/>: Prevents tests from running in parallel, either globally or within specified constraint keys.</description></item>
/// <item><description><see cref="ParallelGroupConstraint"/>: Groups tests together so they run in parallel with each other but not with tests from other groups.</description></item>
/// </list>
/// </para>
/// <para>
/// Constraints are typically applied to tests using attributes like <see cref="NotInParallelAttribute"/> 
/// and <see cref="ParallelGroupAttribute"/>.
/// </para>
/// </remarks>
/// <seealso cref="NotInParallelConstraint"/>
/// <seealso cref="ParallelGroupConstraint"/>
/// <seealso cref="NotInParallelAttribute"/>
/// <seealso cref="ParallelGroupAttribute"/>
public interface IParallelConstraint;