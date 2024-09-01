namespace xUnitTimer;

public class Tests
{
    [Theory, MemberData(nameof(Repeat))]
    public async Task Test1(object _)
    {
        await Task.Delay(50);
    }

    public static IEnumerable<object[]> Repeat()
    {
        return Enumerable.Range(0, 10).Select(i => (object[])[i]);
    }
}