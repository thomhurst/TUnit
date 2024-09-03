namespace TUnitTimer;

public class RepeatTests
{
    [Test, Repeat(99)]
    public async Task Test1()
    {
        await Task.Delay(50);
    }
}