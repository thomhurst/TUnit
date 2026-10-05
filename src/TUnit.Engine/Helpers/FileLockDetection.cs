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
    // as the HResult. EWOULDBLOCK (== EAGAIN) is 11 on Linux and 35 on the BSDs (macOS, FreeBSD,
    // NetBSD, OpenBSD). Both are accepted everywhere rather than probing the OS: the other value is
    // EDEADLK on each family, and treating that as contention only costs a bounded retry.
    private const int EWouldBlockLinux = 11;
    private const int EWouldBlockBsd = 35;

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
        else if (exception.HResult is EWouldBlockLinux or EWouldBlockBsd)
        {
            return true;
        }

        // Best-effort fallback for hosts that map the error differently. The runtime's message is
        // English on .NET (Core); a localized .NET Framework message simply won't match.
        return exception.Message.Contains("being used by another process");
    }
}
