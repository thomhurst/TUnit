using Microsoft.Testing.Extensions.TrxReport.Abstractions;

namespace TUnit.Engine.Capabilities;

internal class TrxReportCapability : ITrxReportCapability
{
    public void Enable()
        => IsTrxEnabled = true;

    public bool IsSupported => true;

    public bool IsTrxEnabled { get; private set; }
}
