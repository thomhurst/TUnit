using TUnit.Core.Enums;

namespace TUnit.Core;

public class GlobalContext : Context
{
    public new static GlobalContext Current { get; } = new();
    
    private GlobalContext()
    {
    }

    public static LogLevel LogLevel { get; internal set; } = LogLevel.Information;
}