using TUnit.Core.Logging;

namespace TUnit.Core.Interfaces;

public interface IContext
{
    TextWriter OutputWriter { get; }
    TextWriter ErrorOutputWriter { get; }
    DefaultLogger GetDefaultLogger();
}