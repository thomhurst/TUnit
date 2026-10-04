using System.Runtime.InteropServices;
using TUnit.Assertions.Extensions;
using TUnit.Core.Enums;

namespace TUnit.UnitTests;

public class OperatingSystemAttributeTests
{
    private static OS? CurrentOS =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? OS.Windows
        : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? OS.Linux
        : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? OS.MacOs
        : null;

    private static OS OtherDesktopOSes(OS current) => (OS.Windows | OS.Linux | OS.MacOs) & ~current;

    [Test]
    public async Task RunOn_Current_OS_Does_Not_Skip()
    {
        Skip.When(CurrentOS is null, "Not running on Windows, Linux or macOS.");

        var skip = await new RunOnAttribute(CurrentOS!.Value).ShouldSkip(null!);

        await Assert.That(skip).IsFalse();
    }

    [Test]
    public async Task RunOn_Other_OSes_Skips()
    {
        Skip.When(CurrentOS is null, "Not running on Windows, Linux or macOS.");

        var skip = await new RunOnAttribute(OtherDesktopOSes(CurrentOS!.Value)).ShouldSkip(null!);

        await Assert.That(skip).IsTrue();
    }

    [Test]
    public async Task ExcludeOn_Current_OS_Skips()
    {
        Skip.When(CurrentOS is null, "Not running on Windows, Linux or macOS.");

        var skip = await new ExcludeOnAttribute(CurrentOS!.Value).ShouldSkip(null!);

        await Assert.That(skip).IsTrue();
    }

    [Test]
    public async Task ExcludeOn_Other_OSes_Does_Not_Skip()
    {
        Skip.When(CurrentOS is null, "Not running on Windows, Linux or macOS.");

        var skip = await new ExcludeOnAttribute(OtherDesktopOSes(CurrentOS!.Value)).ShouldSkip(null!);

        await Assert.That(skip).IsFalse();
    }
}
