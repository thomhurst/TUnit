using Microsoft.Testing.Extensions.TrxReport.Abstractions;

namespace TUnit.Engine.Capabilities;

internal class TrxReportCapability : ITrxReportCapability
{
    public void Enable()
    {
        IsTrxEnabled = true;
    }

    public bool IsTrxEnabled { get; private set; }

    public bool IsSupported => true;
}
