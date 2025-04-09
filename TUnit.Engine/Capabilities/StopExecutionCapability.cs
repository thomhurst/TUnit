using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using TUnit.Core;

namespace TUnit.Engine.Capabilities;

[Experimental("TPEXP")]
public class StopExecutionCapability : IGracefulStopTestExecutionCapability
{
    public AsyncEvent<EventArgs>? OnStopRequested { get; set; }
    
    public async Task StopTestExecutionAsync(CancellationToken cancellationToken)
    {
        IsStopRequested = true;
        
        if (OnStopRequested != null)
        {
            await OnStopRequested.InvokeAsync(this, EventArgs.Empty);
        }
    }

    public bool IsStopRequested
    {
        get;
        private set;
    }
}