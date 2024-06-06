using Microsoft.Testing.Extensions.TrxReport.Abstractions;

namespace TUnit.Engine.Properties;

public class TrxReportProperty : ITrxReportCapability
{
    public void Enable()
    {
    }

    public bool IsSupported => true;
}