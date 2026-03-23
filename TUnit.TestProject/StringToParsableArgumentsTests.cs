namespace TUnit.TestProject;

public class StringToParsableArgumentsTests
{
    [Test]
    [Arguments("2022-5-31")]
    [Arguments("2022-6-1")]
    public async Task DateTime_From_String(DateTime testDate)
    {
        await Assert.That(testDate).IsNotEqualTo(default(DateTime));
    }

    [Test]
    [Arguments("01:30:00")]
    public async Task TimeSpan_From_String(TimeSpan timeSpan)
    {
        await Assert.That(timeSpan).IsNotEqualTo(default(TimeSpan));
    }

    [Test]
    [Arguments("d3b07384-d113-4ec0-8b2a-1e1f0e1e4e57")]
    public async Task Guid_From_String(Guid guid)
    {
        await Assert.That(guid).IsNotEqualTo(Guid.Empty);
    }

    [Test]
    [Arguments("2022-05-31T14:30:00+02:00")]
    public async Task DateTimeOffset_From_String(DateTimeOffset dto)
    {
        await Assert.That(dto).IsNotEqualTo(default(DateTimeOffset));
    }

#if NET8_0_OR_GREATER
    [Test]
    [Arguments("2022-05-31")]
    public async Task DateOnly_From_String(DateOnly date)
    {
        await Assert.That(date).IsNotEqualTo(default(DateOnly));
    }

    [Test]
    [Arguments("14:30:00")]
    public async Task TimeOnly_From_String(TimeOnly time)
    {
        await Assert.That(time).IsNotEqualTo(default(TimeOnly));
    }
#endif
}
