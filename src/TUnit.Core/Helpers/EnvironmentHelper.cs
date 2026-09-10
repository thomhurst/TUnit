namespace TUnit.Core.Helpers;

/// <summary>
/// Provides cached environment information to avoid repeated system calls.
/// </summary>
internal static class EnvironmentHelper
{
    /// <summary>
    /// Cached machine name - avoids system call overhead on every test completion.
    /// Environment.MachineName requires a P/Invoke call which is expensive when called millions of times.
    /// </summary>
    public static readonly string MachineName = Environment.MachineName;
}
