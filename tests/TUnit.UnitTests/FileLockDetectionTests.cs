using TUnit.Assertions.Extensions;
using TUnit.Core.Enums;
using TUnit.Engine.Helpers;

namespace TUnit.UnitTests;

public class FileLockDetectionTests
{
    [Test]
    public async Task Contention_On_An_Exclusively_Held_File_Is_Detected()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tunit-lock-{Guid.NewGuid():N}.txt");

        try
        {
            using var holder = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);

            IOException? caught = null;
            try
            {
                using var contender = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None);
            }
            catch (IOException e)
            {
                caught = e;
            }

            await Assert.That(caught).IsNotNull();
            await Assert.That(FileLockDetection.IsFileLocked(caught!)).IsTrue();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    [RunOn(OS.Windows)]
    public async Task Windows_Sharing_And_Lock_Violations_Are_Detected()
    {
        await Assert.That(FileLockDetection.IsFileLocked(new IOException("x", unchecked((int) 0x80070020)))).IsTrue();
        await Assert.That(FileLockDetection.IsFileLocked(new IOException("x", unchecked((int) 0x80070021)))).IsTrue();
        await Assert.That(FileLockDetection.IsFileLocked(new IOException("x", unchecked((int) 0x80070005)))).IsFalse();
    }

    [Test]
    [ExcludeOn(OS.Windows)]
    public async Task Unix_EWouldBlock_For_This_Platform_Is_Detected()
    {
        // EWOULDBLOCK is 35 on macOS and FreeBSD, 11 on Linux; the other value means something else here.
        var (ewouldblock, other) = OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD() ? (35, 11) : (11, 35);

        await Assert.That(FileLockDetection.IsFileLocked(new IOException("x", ewouldblock))).IsTrue();
        await Assert.That(FileLockDetection.IsFileLocked(new IOException("x", other))).IsFalse();
    }

    [Test]
    public async Task Unrelated_IOException_Is_Not_Treated_As_Locked()
    {
        var exception = new FileNotFoundException("missing", "nope.txt");

        await Assert.That(FileLockDetection.IsFileLocked(exception)).IsFalse();
    }
}
