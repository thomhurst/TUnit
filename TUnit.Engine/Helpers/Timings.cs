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
            
            lock (context.Timings)
            {
                context.Timings.Add(new Timing(name, start, DateTimeOffset.Now));
            }
        }
        catch
        {
            lock (context.Timings)
            {
                context.Timings.Add(new Timing(name, start, DateTimeOffset.Now));
            }

            throw;
        }
    }  
}