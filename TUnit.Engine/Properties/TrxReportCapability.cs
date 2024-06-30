using Microsoft.Testing.Extensions.TrxReport.Abstractions;

namespace TUnit.Engine.Properties;

internal class TrxReportCapability : ITrxReportCapability
{
    public void Enable()
    {
    }

    public bool IsSupported => true;
}