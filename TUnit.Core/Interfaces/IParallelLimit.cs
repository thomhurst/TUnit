namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines an interface for specifying the maximum degree of parallelism during test execution.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IParallelLimit"/> interface is a core component of TUnit's parallel test execution strategy.
/// It provides a mechanism for controlling how many tests can execute concurrently within a test session,
/// allowing for fine-grained control over system resource utilization.
/// </para>
/// <para>
/// Implementations of this interface are typically used in conjunction with the 
/// <see cref="ParallelLimiterAttribute{TParallelLimit}"/> to apply parallelism constraints at different levels:
/// <list type="bullet">
/// <item><description>Assembly level - limiting parallelism across all tests in an assembly</description></item>
/// <item><description>Class level - controlling parallelism for tests within a specific test class</description></item>
/// <item><description>Method level - setting constraints for individual test methods</description></item>
/// </list>
/// </para>
/// <para>
/// Custom implementations can be created to address specific testing scenarios or resource constraints.
/// </para>
/// </remarks>
/// <seealso cref="ParallelLimiterAttribute`1"/>
/// <seealso cref="Helpers.DefaultParallelLimit"/>
/// <seealso cref="IParallelConstraint"/>
public interface IParallelLimit
{
    /// <summary>
    /// Gets the maximum number of tests that can be executed in parallel.
    /// </summary>
    /// <value>
    /// A positive integer representing the maximum number of tests that can run concurrently.
    /// This value is used to create a <see cref="SemaphoreSlim"/> that limits
    /// the degree of parallelism in test execution.
    /// </value>
    /// <remarks>
    /// <para>
    /// The limit controls how many tests can run simultaneously within the test runner.
    /// A higher value allows more tests to execute in parallel, which may speed up test execution
    /// but could increase resource usage.
    /// </para>
    /// <para>
    /// Common implementations include:
    /// <list type="bullet">
    /// <item><description><c>DefaultParallelLimit</c> - Uses <see cref="Environment.ProcessorCount"/> as the limit</description></item>
    /// <item><description>Custom implementations with fixed limits (e.g., <c>ParallelLimit3</c> with a limit of 3)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This property must return a positive value (greater than zero). Returning zero or a negative value
    /// will result in an exception being thrown when the limit is used.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown by the test execution engine when this property returns a value less than or equal to zero.
    /// </exception>
    int Limit { get; }
}