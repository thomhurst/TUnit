namespace NUnitTimer;

[Parallelizable(ParallelScope.All)]
public class RepeatTests
{
    [Test, Repeat(100)]
    public async Task Test1()
    {
        await Task.Delay(50);
    }
}