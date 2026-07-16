using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Specifies that a test method, test class, or assembly should be hidden from test discovery/explorer.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a test method, class, or assembly, the NotDiscoverableAttribute prevents the test(s)
/// from appearing in test explorers and IDE test runners, while still allowing them to execute normally
/// when run via filters or direct invocation.
/// </para>
/// <para>
/// This is useful for infrastructure tests, internal helpers, or tests that should only be run
/// as dependencies of other tests.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple usage - hide test from explorer
/// [Test]
/// [NotDiscoverable]
/// public void InfrastructureSetupTest()
/// {
///     // This test will not appear in test explorer but can still be executed
/// }
///
/// // With reason for documentation
/// [Test]
/// [NotDiscoverable("Internal fixture helper - not meant to be run directly")]
/// public void SharedFixtureSetup()
/// {
///     // Hidden from discovery
/// }
///
/// // Conditional hiding via inheritance
/// public class NotDiscoverableOnCIAttribute : NotDiscoverableAttribute
/// {
///     public NotDiscoverableOnCIAttribute() : base("Hidden on CI") { }
///
///     public override Task&lt;bool&gt; ShouldHide(TestRegisteredContext context)
///     {
///         return Task.FromResult(Environment.GetEnvironmentVariable("CI") == "true");
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false, Inherited = true)]
public class NotDiscoverableAttribute : TUnitAttribute, ITestRegisteredEventReceiver
{
    /// <summary>
    /// Gets the reason why this test is hidden from discovery.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotDiscoverableAttribute"/> class.
    /// </summary>
    public NotDiscoverableAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotDiscoverableAttribute"/> class with a reason.
    /// </summary>
    /// <param name="reason">The reason why this test is hidden from discovery.</param>
    public NotDiscoverableAttribute(string reason)
    {
        Reason = reason;
    }

    /// <inheritdoc />
    public int Order => int.MinValue;

    /// <inheritdoc />
    public async ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        if (await ShouldHide(context))
        {
            context.TestContext.IsNotDiscoverable = true;
        }
    }

    /// <summary>
    /// Determines whether the test should be hidden from discovery.
    /// </summary>
    /// <param name="context">The test context containing information about the test being registered.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result is true if the test should be hidden; otherwise, false.
    /// </returns>
    /// <remarks>
    /// Can be overridden in derived classes to implement conditional hiding logic
    /// based on specific conditions or criteria.
    ///
    /// The default implementation always returns true, meaning the test will always be hidden.
    /// </remarks>
    public virtual Task<bool> ShouldHide(TestRegisteredContext context) => Task.FromResult(true);
}
