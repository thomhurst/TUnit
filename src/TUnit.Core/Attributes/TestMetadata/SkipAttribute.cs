using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Specifies that a test method, test class, or assembly should be skipped during test execution.
/// </summary>
/// <remarks>
/// When applied to a test method, class, or assembly, the SkipAttribute prevents the test(s) from being executed
/// and marks them as skipped with the provided reason.
///
/// Skip can be applied at the method, class, or assembly level.
/// When applied at a class level, all test methods in the class will be skipped.
/// When applied at the assembly level, it affects all tests in the assembly.
/// </remarks>
/// <example>
/// <code>
/// [Test]
/// [Skip("Not implemented yet")]
/// public void TestThatIsNotReady()
/// {
///     // This test will be skipped with the reason "Not implemented yet"
/// }
///
/// // Example of a custom skip attribute with conditional logic
/// public class SkipOnLinuxAttribute : SkipAttribute
/// {
///     public SkipOnLinuxAttribute() : base("Test not supported on Linux")
///     {
///     }
///
///     public override Task&lt;bool&gt; ShouldSkip(TestRegisteredContext context)
///     {
///         return Task.FromResult(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class SkipAttribute : Attribute, ITestRegisteredEventReceiver
{
    public string Reason { get; }

    public SkipAttribute(string reason)
    {
        Reason = reason;
    }

    /// <inheritdoc />
    public int Order => int.MinValue;

    /// <inheritdoc />
    public async ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        if (await ShouldSkip(context))
        {
            context.SetSkipped(GetSkipReason(context));
        }
    }

    /// <summary>
    /// Determines whether a test should be skipped.
    /// </summary>
    /// <param name="context">The test context containing information about the test being registered.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result is true if the test should be skipped; otherwise, false.
    /// </returns>
    /// <remarks>
    /// Can be overridden in derived classes to implement conditional skip logic
    /// based on specific conditions or criteria.
    ///
    /// The default implementation always returns true, meaning the test will always be skipped.
    /// </remarks>
    public virtual Task<bool> ShouldSkip(TestRegisteredContext context) => Task.FromResult(true);

    /// <summary>
    /// Gets the skip reason for the test.
    /// </summary>
    /// <param name="context">The test context containing information about the test being registered.</param>
    /// <returns>The reason why the test should be skipped.</returns>
    /// <remarks>
    /// Can be overridden in derived classes to provide dynamic skip reasons based on runtime information.
    /// This allows including contextual information (e.g., device names, environment variables) in skip messages.
    ///
    /// The default implementation returns the <see cref="Reason"/> property value.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class SkipOnDeviceAttribute : SkipAttribute
    /// {
    ///     private readonly string _deviceName;
    ///
    ///     public SkipOnDeviceAttribute(string deviceName) : base("Device-specific skip")
    ///     {
    ///         _deviceName = deviceName;
    ///     }
    ///
    ///     protected override string GetSkipReason(TestRegisteredContext context)
    ///     {
    ///         return $"Test '{context.TestName}' is not supported on device '{_deviceName}'";
    ///     }
    /// }
    /// </code>
    /// </example>
    protected virtual string GetSkipReason(TestRegisteredContext context) => Reason;
}
