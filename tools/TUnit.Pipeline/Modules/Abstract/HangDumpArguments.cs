using System.Runtime.InteropServices;

namespace TUnit.Pipeline.Modules.Abstract;

/// <summary>
/// Builds the Microsoft.Testing.Platform hang-dump arguments every test module passes.
/// </summary>
public static class HangDumpArguments
{
    /// <summary>
    /// Returns <c>--hangdump</c> with the given dump file name and inactivity timeout. On macOS it also
    /// asks for a heap dump instead of the default full dump: a full dump of an arm64 test process
    /// is about 7.6 GB because it includes every reserved region, and writing one on the 3-core,
    /// 7 GB macOS runner never finished, so a slow-but-passing run sat until the 30-minute module
    /// timeout (#7575). A heap dump of the same process is about 0.5 GB, is written in under a
    /// second, and still holds the async state machines that <c>dotnet-dump analyze</c> needs for
    /// <c>dumpasync</c>. Linux and Windows keep the default full dump on purpose: it finishes there,
    /// and it holds the native memory a heap dump leaves out.
    /// </summary>
    public static string[] Create(string fileName, string timeout) =>
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? ["--hangdump", "--hangdump-filename", fileName, "--hangdump-timeout", timeout, "--hangdump-type", "Heap"]
            : ["--hangdump", "--hangdump-filename", fileName, "--hangdump-timeout", timeout];
}
