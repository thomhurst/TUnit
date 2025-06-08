using System.Runtime.Versioning;

namespace TUnit.Core;

[SupportedOSPlatform("windows")]
public class STAThreadExecutor : DedicatedThreadExecutor
{
    protected override void ConfigureThread(Thread thread)
    {
        thread.SetApartmentState(ApartmentState.STA);
    }
}
