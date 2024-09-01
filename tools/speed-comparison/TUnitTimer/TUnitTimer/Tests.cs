namespace TUnitTimer;

public class Tests
{
    [Test, Repeat(9)]
    public async Task Test1()
    {
        await Task.Delay(50);
    }
}