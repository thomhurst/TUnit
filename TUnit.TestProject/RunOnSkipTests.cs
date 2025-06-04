namespace TUnit.TestProject;

using System.Runtime.InteropServices;
using TUnit.Core.Enums;

public class RunOnSkipTests
{
    [Test]
    [RunOn(OS.Windows)]
    public async Task RunOnWindowsOnlyTest()
    {
        // This test will only run on Windows
        var isSupportedPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        await Assert.That(isSupportedPlatform).IsTrue();
    }

    [Test]
    [RunOn(OS.Windows | OS.Linux | OS.MacOs)]
    public async Task RunOnAllPlatformsTest()
    {
        // This test will run on all supported platforms
        var isSupportedPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                                  RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                                  RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        await Assert.That(isSupportedPlatform).IsTrue();
    }
}
