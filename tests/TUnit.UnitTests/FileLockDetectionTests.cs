using TUnit.Assertions.Extensions;
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
    public async Task Unrelated_IOException_Is_Not_Treated_As_Locked()
    {
        var exception = new FileNotFoundException("missing", "nope.txt");

        await Assert.That(FileLockDetection.IsFileLocked(exception)).IsFalse();
    }
}
