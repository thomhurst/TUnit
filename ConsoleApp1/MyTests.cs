using System.Net.Http.Json;
using FluentAssertions;

namespace ConsoleApp1;

public class MyTests : MyTestBase
{
    [Test]
    public async Task Test1()
    {
        var test = await Client.GetAsync("/weatherforecast");
        var expected = 1;
        var actual = 1;
        expected.Should().Be(actual);
    }

    [Test]
    public void Test2()
    {
        var expected = 1;
        var actual = 1;
        expected.Should().Be(actual);
    }
}
