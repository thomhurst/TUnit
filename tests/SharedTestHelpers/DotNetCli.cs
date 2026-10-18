using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace TUnit.Tests.Shared;

/// <summary>
/// Resolves the <c>dotnet</c> executable used to start child <c>dotnet build</c>/<c>dotnet run</c> processes.
/// </summary>
/// <remarks>
/// A bare <c>dotnet</c> resolves through <c>PATH</c>, which an IDE does not necessarily share with the
/// installation it builds and runs tests with (on macOS, apps started from the Dock or Finder get a different
/// <c>PATH</c> than a terminal). That <c>dotnet</c> can lack an SDK satisfying <c>global.json</c> and exit with
/// 155 (<c>SdkResolveFailure</c>). Prefer the installation that is running the current test process, as long as it
/// has an SDK: a runtime-only installation cannot run
/// <c>dotnet build</c>, so <c>PATH</c> is the better bet there.
/// </remarks>
internal static class DotNetCli
{
    public static string Path { get; } = Resolve();

    private static string Resolve()
    {
        // Set by the .NET CLI for processes it starts (e.g. under `dotnet test`).
        var hostPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrEmpty(hostPath) && File.Exists(hostPath))
        {
            return hostPath;
        }

        // <root>/shared/Microsoft.NETCore.App/<version>/ -> <root>/dotnet
        var runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();
        var root = System.IO.Path.GetFullPath(System.IO.Path.Combine(runtimeDirectory, "..", "..", ".."));
        var muxer = System.IO.Path.Combine(root, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet");

        return File.Exists(muxer) && HasSdk(root) ? muxer : "dotnet";
    }

    private static bool HasSdk(string root)
    {
        var sdkDirectory = System.IO.Path.Combine(root, "sdk");
        return Directory.Exists(sdkDirectory) && Directory.EnumerateDirectories(sdkDirectory).Any();
    }
}
