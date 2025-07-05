using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TUnit.Core.Enums;

namespace TUnit.Core;

/// <summary>
/// Attribute that restricts a test to run only on specific operating systems.
/// </summary>
/// <param name="OperatingSystem">
/// Defines the operating systems on which the test should run.
/// </param>
/// <remarks>
/// <para>
/// The <see cref="RunOnAttribute"/> is used to specify that a test should only run on certain operating systems.
/// Tests with this attribute will be skipped on operating systems that do not match the specified criteria.
/// </para>
/// <para>
/// You can specify multiple operating systems by combining the <see cref="OS"/> enum values with the bitwise OR operator.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Run only on Windows
/// [Test, RunOn(OS.Windows)]
/// public void WindowsOnlyTest()
/// {
///     // This test will only run on Windows
/// }
/// 
/// // Run on both Windows and Linux
/// [Test, RunOn(OS.Windows | OS.Linux)]
/// public void WindowsAndLinuxTest()
/// {
///     // This test will run on Windows and Linux, but not on macOS
/// }
/// 
/// // Run on all supported platforms
/// [Test, RunOn(OS.Windows | OS.Linux | OS.MacOs)]
/// public void AllPlatformsTest()
/// {
///     // This test will run on all supported platforms
/// }
/// </code>
/// </example>
/// <seealso cref="SkipAttribute"/>
/// <seealso cref="OS"/>
public sealed class RunOnAttribute(OS OperatingSystem) : SkipAttribute($"Test is restricted to run on the following operating systems: `{OperatingSystem}`.")
{
    /// <inheritdoc />
    public override Task<bool> ShouldSkip(TestRegisteredContext context)
    {
        // Check if the current platform matches any of the allowed operating systems
        var shouldRun =
            (OperatingSystem.HasFlag(OS.Windows) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#if NET
            // Only validate Linux and macOS on .NET 5+ where these OS flags are available
            || (OperatingSystem.HasFlag(OS.Linux) && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            || (OperatingSystem.HasFlag(OS.MacOs) && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
#endif
            ;

        // Return true if the test should be skipped (opposite of shouldRun)
        return Task.FromResult(!shouldRun);
    }
}
