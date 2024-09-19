using TUnit.Core.Logging;

namespace TUnit.Core.Interfaces;

public interface IContext
{
    List<TUnitLogger> Loggers { get; }

#if NET8_0_OR_GREATER
    public TUnitLogger GetDefaultLogger()
    {
        return RegisterLogger(new DefaultLogger());
    }
    
    public TUnitLogger RegisterLogger(TUnitLogger logger)
    {
        Loggers.Add(logger);
        return logger;
    }
#endif
}