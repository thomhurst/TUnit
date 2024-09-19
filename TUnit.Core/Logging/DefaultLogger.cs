namespace TUnit.Core.Logging;

internal class DefaultLogger : TUnitLogger
{
    protected override void Log(string message)
    {
        Console.WriteLine(message);
    }
}