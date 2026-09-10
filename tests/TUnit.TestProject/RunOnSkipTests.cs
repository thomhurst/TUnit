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

    [Test]
    [RunOn(OS.Browser)]
    public async Task RunOnBrowserOnlyTest()
    {
        // This test will only run on Browser platform
#if NET
        var isBrowserPlatform = System.OperatingSystem.IsBrowser();
        await Assert.That(isBrowserPlatform).IsTrue();
#else
        // For frameworks that don't have OperatingSystem.IsBrowser(), assume not browser
        await Assert.That(false).IsTrue();
#endif
    }

    [Test]
    [ExcludeOn(OS.Browser)]
    public async Task ExcludeOnBrowserTest()
    {
        // This test will run on all platforms except Browser
#if NET
        var isBrowserPlatform = System.OperatingSystem.IsBrowser();
        await Assert.That(isBrowserPlatform).IsFalse();
#else
        // For frameworks that don't have OperatingSystem.IsBrowser(), assume not browser
        await Assert.That(true).IsTrue();
#endif
    }
}
