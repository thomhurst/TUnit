using System;
using System.Diagnostics;
using System.IO;
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
/// resolves an SDK for this repository: a runtime-only installation, or one whose SDKs don't satisfy
/// <c>global.json</c>, cannot run <c>dotnet build</c> here, so <c>PATH</c> is the better bet there.
/// </remarks>
internal static class DotNetCli
{
    // Resolved on first use rather than in the type initializer, so an unexpected failure surfaces as itself
    // instead of a TypeInitializationException that also breaks every later use of the type
    private static readonly Lazy<string> ResolvedPath = new(Resolve);

    /// <summary>The <c>dotnet</c> executable to start child processes with: a full path, or <c>dotnet</c> to use <c>PATH</c>.</summary>
    public static string Path => ResolvedPath.Value;

    /// <summary>
    /// Prefers <c>DOTNET_HOST_PATH</c>, then the muxer of the installation running this process when it resolves an
    /// SDK for this repository, and otherwise falls back to <c>dotnet</c> on <c>PATH</c>.
    /// </summary>
    private static string Resolve()
    {
        // Set by the .NET CLI for processes it starts (e.g. under `dotnet test`), so it already resolved an SDK.
        var hostPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrEmpty(hostPath) && File.Exists(hostPath))
        {
            return hostPath;
        }

        // <root>/shared/Microsoft.NETCore.App/<version>/ -> <root>/dotnet
        var runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();
        var root = System.IO.Path.GetFullPath(System.IO.Path.Combine(runtimeDirectory, "..", "..", ".."));
        var muxer = System.IO.Path.Combine(root, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet");

        return File.Exists(muxer) && ResolvesSdk(muxer) ? muxer : "dotnet";
    }

    /// <summary>
    /// Whether <paramref name="muxer"/> resolves an SDK from the test assembly's directory, which lies inside the
    /// repository: <c>dotnet --version</c> applies <c>global.json</c> the same way the child builds do and fails, with
    /// 155, exactly when they would.
    /// </summary>
    private static bool ResolvesSdk(string muxer)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(muxer, "--version")
            {
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            });

            if (process is null)
            {
                return false;
            }

            // Drain and discard both streams so neither can fill up and block the process
            process.OutputDataReceived += static (_, _) => { };
            process.ErrorDataReceived += static (_, _) => { };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (!process.WaitForExit(30_000))
            {
                process.Kill(entireProcessTree: true);
                return false;
            }

            return process.ExitCode == 0;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
        {
            return false;
        }
    }
}
