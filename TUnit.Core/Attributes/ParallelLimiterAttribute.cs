using TUnit.Core.Contexts;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Limits the number of tests that can run in parallel for a test assembly, class, or method.
/// </summary>
/// <typeparam name="TParallelLimit">
/// The type that implements <see cref="IParallelLimit"/> and defines the maximum number 
/// of tests that can execute concurrently.
/// </typeparam>
/// <remarks>
/// <para>
/// This attribute controls the degree of parallelism for test execution. When applied to a test assembly,
/// class, or method, it limits how many tests from that scope can run simultaneously.
/// </para>
/// <para>
/// The parallelism limit is defined by the <see cref="IParallelLimit.Limit"/> property of the 
/// <typeparamref name="TParallelLimit"/> instance. This value is used to create a semaphore that
/// controls concurrent test execution.
/// </para>
/// <para>
/// Common implementations include:
/// <list type="bullet">
/// <item><description><c>DefaultParallelLimit</c> - Uses <see cref="Environment.ProcessorCount"/> as the limit</description></item>
/// <item><description>Custom implementations with fixed limits (e.g., <c>ParallelLimit3</c> with a limit of 3)</description></item>
/// </list>
/// </para>
/// <para>
/// Example usage:
/// <code>
/// // Apply to an assembly to limit all tests
/// [assembly: ParallelLimiter&lt;DefaultParallelLimit&gt;]
/// 
/// // Apply to a class to limit tests in that class
/// [ParallelLimiter&lt;ParallelLimit3&gt;]
/// public class MyTestClass
/// {
///     // Tests in this class will run with a maximum of 3 in parallel
/// }
/// 
/// // Apply to a specific test method
/// [Test]
/// [ParallelLimiter&lt;ParallelLimit1&gt;]
/// public void MyTest()
/// {
///     // This test will run exclusively (limit of 1)
/// }
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ParallelLimiterAttribute<TParallelLimit> : TUnitAttribute, ITestRegisteredEventReceiver
    where TParallelLimit : IParallelLimit, new()
{
    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.SetParallelLimiter(new TParallelLimit());
        return default;
    }
};