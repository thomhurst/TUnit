using TUnit.Assertions.Should;
using TUnit.Assertions.Should.Extensions;

namespace TUnit.Assertions.Should.Tests;

public class DateTimeTests
{
    [Test]
    public async Task DateTime_BeEqualTo()
    {
        var dt = new DateTime(2026, 4, 28, 12, 0, 0);
        await dt.Should().BeEqualTo(dt);
    }

    [Test]
    public async Task DateTime_EqualExact()
    {
        var dt = new DateTime(2026, 4, 28, 12, 0, 0);
        await dt.Should().EqualExact(dt);
    }

    [Test]
    public async Task DateTimeOffset_BeEqualTo()
    {
        var dto = DateTimeOffset.Parse("2026-04-28T12:00:00+00:00");
        await dto.Should().BeEqualTo(dto);
    }

    [Test]
    public async Task DateOnly_BeEqualTo()
    {
        var date = new DateOnly(2026, 4, 28);
        await date.Should().BeEqualTo(date);
    }

    [Test]
    public async Task TimeOnly_BeEqualTo()
    {
        var time = new TimeOnly(12, 0, 0);
        await time.Should().BeEqualTo(time);
    }

    [Test]
    public async Task TimeSpan_BeEqualTo()
    {
        var span = TimeSpan.FromMinutes(5);
        await span.Should().BeEqualTo(span);
    }
}
