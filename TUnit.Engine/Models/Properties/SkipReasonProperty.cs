using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Models.Properties;

public class SkipReasonProperty : IProperty
{
    public string SkipReason { get; }

    public SkipReasonProperty(string skipReason)
    {
        SkipReason = skipReason;
    }
}