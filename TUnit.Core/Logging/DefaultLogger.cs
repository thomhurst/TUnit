using TUnit.Core.Interfaces;

namespace TUnit.Core.Logging;

internal class DefaultLogger : TUnitLogger
{
    // Console Interceptor automatically writes to the context so we don't want to duplicate!
    protected override bool WriteToContext => false;

    protected override void Log(IContext? currentContext, string message)
    {
        Console.WriteLine(message);
    }
}