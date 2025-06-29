namespace TUnit.Core.Enums;

/// <summary>
/// Represents operating systems that can be specified for test execution constraints.
/// </summary>
/// <remarks>
/// <para>
/// This enum is marked with the <see cref="FlagsAttribute"/>, which allows combining multiple operating systems
/// using bitwise operations when used with attributes like <see cref="RunOnAttribute"/>.
/// </para>
/// <para>
/// The primary use case is to restrict test execution to specific operating systems through
/// the <see cref="RunOnAttribute"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Specify a test should run only on Windows
/// [RunOn(OS.Windows)]
/// 
/// // Specify a test should run on either Windows or Linux
/// [RunOn(OS.Windows | OS.Linux)]
/// 
/// // Specify a test should run on all supported platforms
/// [RunOn(OS.Windows | OS.Linux | OS.MacOs)]
/// </code>
/// </example>
/// <seealso cref="RunOnAttribute"/>
[Flags]
public enum OS
{
    /// <summary>
    /// Represents the Linux operating system.
    /// </summary>
    /// <remarks>
    /// Tests with this flag will be executed on Linux platforms when used with <see cref="RunOnAttribute"/>.
    /// </remarks>
    Linux = 1,

    /// <summary>
    /// Represents the Windows operating system.
    /// </summary>
    /// <remarks>
    /// Tests with this flag will be executed on Windows platforms when used with <see cref="RunOnAttribute"/>.
    /// </remarks>
    Windows = 2,

    /// <summary>
    /// Represents the macOS operating system.
    /// </summary>
    /// <remarks>
    /// Tests with this flag will be executed on macOS platforms when used with <see cref="RunOnAttribute"/>.
    /// </remarks>
    MacOs = 4
}
