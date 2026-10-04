using System.Runtime.InteropServices;

namespace TUnit.Pipeline;

internal static class RuntimeIdentifiers
{
    /// <summary>
    /// The portable runtime identifier of the machine running the pipeline, e.g. <c>linux-arm64</c>.
    /// Native AOT cannot cross-compile to another OS, so AOT publishes always target the host.
    /// Built from the OS and process architecture rather than <see cref="RuntimeInformation.RuntimeIdentifier"/>,
    /// which reports a distro-specific RID (e.g. <c>ubuntu.24.04-x64</c>) on source-built SDKs.
    /// </summary>
    public static string Current { get; } = $"{CurrentOperatingSystem}-{CurrentArchitecture}";

    private static string CurrentOperatingSystem =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win"
        : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx"
        : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux"
        : throw new PlatformNotSupportedException($"No runtime identifier for {RuntimeInformation.OSDescription}.");

    private static string CurrentArchitecture => RuntimeInformation.ProcessArchitecture switch
    {
        Architecture.X64 => "x64",
        Architecture.Arm64 => "arm64",
        var other => throw new PlatformNotSupportedException($"No runtime identifier for architecture {other}."),
    };
}
