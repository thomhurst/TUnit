using TUnit.Core.Logging;

namespace TUnit.Core.Interfaces;

public interface IHasLoggers
{
    public List<TUnitLogger> Loggers { get; }
}