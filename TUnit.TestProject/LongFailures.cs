namespace TUnit.TestProject;

[Category("LongFailures")]
public class LongFailures
{
    private static int _counter;
    
    [Repeat(100)]
    [Test]
    public async Task LongFailure()
    {
        await Task.Delay(TimeSpan.FromSeconds(Interlocked.Increment(ref _counter)));
        throw new Exception();
    }
}