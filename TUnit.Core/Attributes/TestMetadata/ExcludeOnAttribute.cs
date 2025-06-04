using System.Runtime.InteropServices;
using TUnit.Core.Enums;

namespace TUnit.Core;

/// <summary>
/// Attribute that excludes a test from running on specific operating systems.
/// </summary>
/// <param name="OperatingSystem">
/// Defines the operating systems on which the test should not run.
/// </param>
/// <remarks>
/// <para>
/// The <see cref="ExcludeOnAttribute"/> is used to specify that a test should not run on certain operating systems.
/// Tests with this attribute will be skipped on operating systems that match the specified criteria.
/// </para>
/// <para>
/// You can specify multiple operating systems by combining the <see cref="OS"/> enum values with the bitwise OR operator.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Skip on Windows
/// [Test, ExcludeOn(OS.Windows)]
/// public void NonWindowsOnlyTest()
/// {
///     // This test will run on Linux and macOS, but not on Windows
/// }
/// 
/// // Skip on both Windows and Linux
/// [Test, ExcludeOn(OS.Windows | OS.Linux)]
/// public void MacOsOnlyTest()
/// {
///     // This test will only run on macOS
/// }
/// 
/// // Skip on all supported platforms (essentially always skips the test)
/// [Test, ExcludeOn(OS.Windows | OS.Linux | OS.MacOs)]
/// public void NeverRunTest()
/// {
///     // This test will not run on any supported platform
/// }
/// </code>
/// </example>
/// <seealso cref="SkipAttribute"/>
/// <seealso cref="RunOnAttribute"/>
/// <seealso cref="OS"/>
public sealed class ExcludeOnAttribute(OS OperatingSystem) : SkipAttribute(GetReason(OperatingSystem))
{
    /// <inheritdoc />
    public override Task<bool> ShouldSkip(BeforeTestContext context)
    {
        // Check if the current platform matches any of the excluded operating systems
        bool shouldSkip =
            (OperatingSystem.HasFlag(OS.Windows) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#if NET
            // Only validate Linux and macOS on .NET 5+ where these OS flags are available
            || (OperatingSystem.HasFlag(OS.Linux) && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            || (OperatingSystem.HasFlag(OS.MacOs) && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
#endif
            ;

        // Return true if the test should be skipped (if we're on an excluded OS)
        return Task.FromResult(shouldSkip);
    }

    private static string GetReason(OS operatingSystems)
    {
        return $"This test is excluded on the following operating systems: `{operatingSystems}`.";
    }
}