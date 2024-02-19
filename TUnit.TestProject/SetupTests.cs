using System.Diagnostics;
using System.Net;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

public class Base1
{
    [OnlyOnceSetUp]
    public static async Task Setup1()
    {
    }
}

public class Base2 : Base1
{
    [OnlyOnceSetUp]
    public static async Task Setup2()
    {
    }
}

public class Base3 : Base2
{
    [OnlyOnceSetUp]
    public static async Task Setup3()
    {
    }
}

public class SetupTests : Base3
{
    private int _value;
    private static HttpResponseMessage? _pingResponse;

    [OnlyOnceSetUp]
    public static async Task Ping()
    {
        //_pingResponse = await new HttpClient().GetAsync("https://localhost/ping");
    }
    
    [SetUp]
    public async Task Setup()
    {
        await new HttpClient().GetAsync($"https://localhost/test-finished-notifier?testName={TestContext.Current.TestInformation.TestName}");
    }

    [Test]
    public async Task Test()
    {
        await Assert.That(_value).Is.EqualTo(99);
        await Assert.That(_pingResponse?.StatusCode).Is.Not.Null().And.Is.EqualTo(HttpStatusCode.OK);
    }
}