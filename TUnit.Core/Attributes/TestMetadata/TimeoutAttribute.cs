using System.Threading.Tasks;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Attribute that specifies a timeout for test execution.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute to enforce time limits on test execution. Tests that exceed
/// the specified timeout will be canceled and marked as failed.
/// </para>
/// 
/// <para>
/// The attribute can be applied at different levels:
/// </para>
/// <list type="bullet">
/// <item>Method level: Sets timeout for a specific test method</item>
/// <item>Class level: Sets timeout for all test methods in the class (unless overridden at method level)</item>
/// <item>Assembly level: Sets timeout for all test methods in the assembly (unless overridden at class or method level)</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Set a 5-second timeout for this test
/// [Test, Timeout(5000)]
/// public async Task TestWithTimeout()
/// {
///     // Test will fail if execution exceeds 5 seconds
///     await Task.Delay(4000); // OK - completes within timeout
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class TimeoutAttribute(int timeoutInMilliseconds) : TUnitAttribute, ITestDiscoveryEventReceiver, ITestRegisteredEventReceiver
{
    /// <inheritdoc />
    public int Order => 0;

    /// <summary>
    /// Gets the timeout period for the test.
    /// </summary>
    /// <value>A <see cref="TimeSpan"/> representing the maximum allowed execution time.</value>
    public TimeSpan Timeout { get; } = TimeSpan.FromMilliseconds(timeoutInMilliseconds);
    
    /// <inheritdoc />
    public void OnTestDiscovery(DiscoveredTestContext discoveredTestContext)
    {
        discoveredTestContext.TestDetails.Timeout = Timeout;
    }

    /// <inheritdoc />
    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        // Apply the timeout to the test when it's registered
        // The test runner will use this value to enforce the timeout during execution
        context.TestDetails.Timeout = Timeout;
        return new ValueTask();
    }
}