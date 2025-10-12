using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class TimeZoneInfoAssertionTests
{
    [Test]
    public async Task Test_TimeZoneInfo_SupportsDaylightSavingTime()
    {
        var timezone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        await Assert.That(timezone).SupportsDaylightSavingTime();
    }

    [Test]
    public async Task Test_TimeZoneInfo_DoesNotSupportDaylightSavingTime()
    {
        var utc = TimeZoneInfo.Utc;
        await Assert.That(utc).DoesNotSupportDaylightSavingTime();
    }

#if NET6_0_OR_GREATER
    [Test]
    public async Task Test_TimeZoneInfo_HasIanaId()
    {
        // Try to get a timezone that has an IANA ID
        var timezone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        await Assert.That(timezone).HasIanaId();
    }

    [Test]
    public async Task Test_TimeZoneInfo_DoesNotHaveIanaId()
    {
        // Windows-style IDs don't have IANA IDs
        var timezone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        await Assert.That(timezone).DoesNotHaveIanaId();
    }
#endif
}
