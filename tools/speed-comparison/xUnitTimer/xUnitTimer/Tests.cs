namespace xUnitTimer;

public class Tests : IClassFixture<Timer>
{
    public Tests(Timer timer)
    {
    }
    
    [Theory, MemberData(nameof(Repeat))]
    public async Task Test1(object _)
    {
        await Task.CompletedTask;
    }

    public static IEnumerable<object[]> Repeat()
    {
        foreach (var i in Enumerable.Range(0, 1001))
        {
            yield return [i];
        }
    }
}