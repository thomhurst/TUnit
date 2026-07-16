namespace TUnit.TestProject;

using System.Runtime.InteropServices;
using TUnit.Core.Enums;

public class ExcludeOnTests
{
    [Test]
    [ExcludeOn(OS.Windows)]
    public async Task ExcludeOnWindowsOnlyTest()
    {
        // This test will not run on Windows
        var isSupportedPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                                  RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        await Assert.That(isSupportedPlatform).IsTrue();
    }

    [Test]
    [ExcludeOn(OS.Windows | OS.Linux | OS.MacOs)]
    public void ExcludeOnAllPlatformsTest()
    {
        Assert.Fail("This message should never be seen.");
    }
}
