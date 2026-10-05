using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using TUnit.Core.Exceptions;

namespace TUnit.Core;

[SupportedOSPlatform("windows")]
public class STAThreadExecutor : DedicatedThreadExecutor
{
    protected override void ConfigureThread(Thread thread)
    {
        // [SupportedOSPlatform] only warns at compile time; without this guard a test run on
        // Linux or macOS gets a bare PlatformNotSupportedException that names neither the
        // executor nor the fix.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new TUnitException(
                $"{nameof(STAThreadExecutor)} requires Windows: STA apartment threads are not supported on {RuntimeInformation.OSDescription}. " +
                "Restrict the test with [RunOn(OS.Windows)] or use a different executor on this platform.");
        }

        thread.SetApartmentState(ApartmentState.STA);
    }
}
