using TUnit.Engine.Logging;
using TUnit.Engine.Services;

namespace TUnit.Engine.Framework;

/// <summary>
/// Provides access to logging, messaging, and output verbosity services.
/// </summary>
internal interface ILoggingServices
{
    TUnitFrameworkLogger Logger { get; }
    VerbosityService VerbosityService { get; }
    TUnitMessageBus MessageBus { get; }
}
