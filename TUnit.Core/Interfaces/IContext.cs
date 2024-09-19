using TUnit.Core.Logging;

namespace TUnit.Core.Interfaces;

public interface IContext
{
    StringWriter OutputWriter { get; }
    StringWriter ErrorOutputWriter { get; }
    
    TUnitLogger RegisterLogger(TUnitLogger logger);
    TUnitLogger GetDefaultLogger();
}