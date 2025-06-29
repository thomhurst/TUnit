using Microsoft.Testing.Extensions.TrxReport.Abstractions;

namespace TUnit.Engine.Capabilities;

internal class TrxReportCapability : ITrxReportCapability
{
    public void Enable()
    {
    }

    public bool IsSupported => true;
}
