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
    // as the HResult. EWOULDBLOCK (== EAGAIN) differs between platforms.
    private const int EWouldBlockLinux = 11;
    private const int EWouldBlockMacOS = 35;

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
        else if (exception.HResult == (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? EWouldBlockMacOS : EWouldBlockLinux))
        {
            return true;
        }

        // Fallback heuristic for hosts that map the error differently.
        return exception.Message.Contains("being used by another process");
    }
}
