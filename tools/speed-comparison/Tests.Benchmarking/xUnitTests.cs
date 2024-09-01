using Xunit;

namespace Tests.Benchmarking;

public class xUnitTests : IClassFixture<Timer>
{
    public xUnitTests(Timer timer)
    {
    }
    
    [Theory, MemberData(nameof(Repeat))]
    public async Task Test1(object _)
    {
        await Task.Delay(50);
    }

    public static IEnumerable<object[]> Repeat()
    {
        foreach (var i in Enumerable.Range(0, 1001))
        {
            yield return [i];
        }
    }
}