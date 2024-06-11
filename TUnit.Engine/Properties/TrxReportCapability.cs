using Microsoft.Testing.Extensions.TrxReport.Abstractions;

namespace TUnit.Engine.Properties;

public class TrxReportCapability : ITrxReportCapability
{
    public void Enable()
    {
    }

    public bool IsSupported => true;
}