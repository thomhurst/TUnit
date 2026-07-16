namespace TUnit.Core;

public abstract class ArgumentDisplayFormatter
{
    public abstract bool CanHandle(object? value);
    public abstract string FormatValue(object? value);
}
