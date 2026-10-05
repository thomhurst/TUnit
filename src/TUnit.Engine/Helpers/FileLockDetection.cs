using System.Runtime.InteropServices;

namespace TUnit.Engine.Helpers;

/// <summary>
/// Recognises the <see cref="IOException"/> thrown when another process holds a file, so report
/// writers can retry instead of giving up.
/// </summary>
internal static class FileLockDetection
{
    // Windows: the low word of the HResult is the Win32 error.
    private const int ErrorSharingViolation = 0x20;
    private const int ErrorLockViolation = 0x21;

    // Unix: .NET takes an advisory flock for FileShare and reports contention with the raw errno
    // as the HResult. EWOULDBLOCK (== EAGAIN) is 11 on Linux and 35 on the BSD family (macOS, FreeBSD).
    private const int EWouldBlockLinux = 11;
    private const int EWouldBlockBsd = 35;

    // OSPlatform.FreeBSD is not part of netstandard2.0.
    private static readonly OSPlatform FreeBSD = OSPlatform.Create("FREEBSD");

    internal static bool IsFileLocked(IOException exception)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var errorCode = exception.HResult & 0xFFFF;

            if (errorCode is ErrorSharingViolation or ErrorLockViolation)
            {
                return true;
            }
        }
        else if (exception.HResult == (IsBsd() ? EWouldBlockBsd : EWouldBlockLinux))
        {
            return true;
        }

        // Best-effort fallback for hosts that map the error differently. The runtime's message is
        // English on .NET (Core); a localized .NET Framework message simply won't match.
        return exception.Message.Contains("being used by another process");
    }

    private static bool IsBsd() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(FreeBSD);
}
