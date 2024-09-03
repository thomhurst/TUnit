namespace xUnitTimer;

public class RepeatTests
{
    [Theory, MemberData(nameof(Repeat))]
    public async Task Test1(object _)
    {
        await Task.Delay(50);
    }

    public static IEnumerable<object[]> Repeat()
    {
        return Enumerable.Range(0, 100).Select(i => (object[])[i]);
    }
}