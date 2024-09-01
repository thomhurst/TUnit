namespace NUnitTimer;

[Parallelizable(ParallelScope.All)]
public class Tests
{
    [Test, Repeat(10)]
    public async Task Test1()
    {
        await Task.Delay(50);
    }
}