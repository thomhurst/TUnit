using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Attribute that marks a test method, class, or assembly to be skipped during test execution.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute when you want to temporarily disable tests without removing them.
/// Tests marked with this attribute will be discovered but not executed.
/// </para>
///
/// <para>
/// The attribute can be applied at different levels:
/// </para>
/// <list type="bullet">
/// <item>Method level: Skips a specific test method</item>
/// <item>Class level: Skips all test methods in the class</item>
/// <item>Assembly level: Skips all test methods in the assembly</item>
/// </list>
///
/// You can extend this attribute to create conditional skip logic by overriding the
/// <see cref="ShouldSkip"/> method.
/// </remarks>
/// <example>
/// <code>
/// // Simple skip
/// [Test, Skip("Not implemented yet")]
/// public void TestNotReadyYet() { }
///
/// // Conditional skip (by creating a derived attribute)
/// public class RunOnWindowsOnlyAttribute : SkipAttribute
/// {
///     public RunOnWindowsOnlyAttribute() : base("Test only runs on Windows") { }
///
///     public override Task&lt;bool&gt; ShouldSkip(BeforeTestContext context)
///     {
///         return Task.FromResult(!OperatingSystem.IsWindows());
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class SkipAttribute(string reason) : TUnitAttribute, ITestRegisteredEventReceiver
{
    /// <summary>
    /// Gets the reason why the test is being skipped.
    /// </summary>
    /// <value>A string explaining why the test is skipped.</value>
    public string Reason { get; protected set; } = reason;

    /// <inheritdoc />
    public int Order => int.MinValue;

    /// <inheritdoc />
    public async ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        if (await ShouldSkip(context))
        {
            context.SkipTest(Reason);
        }
    }

    /// <summary>
    /// Determines whether the test should be skipped.
    /// </summary>
    /// <param name="context">The context providing information about the test.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a boolean value indicating whether the test should be skipped.
    /// </returns>
    /// <remarks>
    /// The base implementation always returns true, meaning the test will always be skipped.
    /// Override this method in derived classes to implement conditional skip logic.
    /// </remarks>
    public virtual Task<bool> ShouldSkip(BeforeTestContext context) => Task.FromResult(true);
}