using TUnit.Core;

namespace TUnit.Engine.Helpers;

internal static class Timings
{
    public static async Task Record(string name, TestContext context, Func<Task> action)
    {
        var start = DateTimeOffset.Now;

        try
        {
            await action();
        }
        finally
        {
            var end = DateTimeOffset.Now;
            
            lock (context.Lock)
            {
                context.Timings.Add(new Timing(name, start, end));
            }
        }
    }  
}