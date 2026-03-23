namespace TUnit.TestProject;

public class StringToParsableArgumentsTests
{
    [Test]
    [Arguments("2022-5-31")]
    public async Task DateTime_From_String(DateTime testDate)
    {
        await Assert.That(testDate).IsEqualTo(new DateTime(2022, 5, 31));
    }

    [Test]
    [Arguments("01:30:00")]
    public async Task TimeSpan_From_String(TimeSpan timeSpan)
    {
        await Assert.That(timeSpan).IsEqualTo(new TimeSpan(1, 30, 0));
    }

    [Test]
    [Arguments("d3b07384-d113-4ec0-8b2a-1e1f0e1e4e57")]
    public async Task Guid_From_String(Guid guid)
    {
        await Assert.That(guid).IsEqualTo(new Guid("d3b07384-d113-4ec0-8b2a-1e1f0e1e4e57"));
    }

    [Test]
    [Arguments("2022-05-31T14:30:00+02:00")]
    public async Task DateTimeOffset_From_String(DateTimeOffset dto)
    {
        await Assert.That(dto).IsEqualTo(new DateTimeOffset(2022, 5, 31, 14, 30, 0, TimeSpan.FromHours(2)));
    }

#if NET8_0_OR_GREATER
    [Test]
    [Arguments("2022-05-31")]
    public async Task DateOnly_From_String(DateOnly date)
    {
        await Assert.That(date).IsEqualTo(new DateOnly(2022, 5, 31));
    }

    [Test]
    [Arguments("14:30:00")]
    public async Task TimeOnly_From_String(TimeOnly time)
    {
        await Assert.That(time).IsEqualTo(new TimeOnly(14, 30, 0));
    }
#endif
}
