namespace TUnit.Engine.Tests;

public static class ThreadSafeOutput
{
    private static readonly object OutputLock = new();

    public static void WriteLine(string value)
    {
        lock (OutputLock)
        {
            Console.WriteLine(value);
        }
    }

    public static void WriteLine(string format, params object[] args)
    {
        lock (OutputLock)
        {
            Console.WriteLine(format, args);
        }
    }

    public static void WriteMultipleLines(params string[] lines)
    {
        lock (OutputLock)
        {
            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }
        }
    }

    public static void WriteBlock(Action<TextWriter> writeAction)
    {
        lock (OutputLock)
        {
            writeAction(Console.Out);
        }
    }
}