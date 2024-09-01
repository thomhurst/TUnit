namespace TUnitTimer;

public class Tests
{
    [Test, Repeat(99)]
    public async Task Test1()
    {
        await Task.Delay(50);
    }
}