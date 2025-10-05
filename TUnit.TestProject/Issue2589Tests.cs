namespace TUnit.TestProject;

/// <summary>
/// Tests for issue #2589: Data driven tests should use params keyword
/// https://github.com/thomhurst/TUnit/issues/2589
/// </summary>
public class Issue2589Tests
{
    [Test]
    [Arguments("sample", "param1", "param2", "param3")]
    public async Task SampleWithParamsArguments(string text, params string[] tags)
    {
        await Assert.That(text).IsEqualTo("sample");
        await Assert.That(tags).IsNotNull();
        await Assert.That(tags.Length).IsEqualTo(3);
        await Assert.That(tags[0]).IsEqualTo("param1");
        await Assert.That(tags[1]).IsEqualTo("param2");
        await Assert.That(tags[2]).IsEqualTo("param3");
    }

    [Test]
    [Arguments("sample", null)]
    public async Task SampleWithNull(string text, params string[]? tags)
    {
        await Assert.That(text).IsEqualTo("sample");
        await Assert.That(tags).IsNull();
    }

    [Test]
    [Arguments("sample", null)]
    public async Task NonParamsWithNull(string text, string[]? tags)
    {
        await Assert.That(text).IsEqualTo("sample");
        await Assert.That(tags).IsNull();
    }

    [Test]
    [Arguments("sample")]
    public async Task ParamsWithNoExtraArgs(string text, params string[] tags)
    {
        await Assert.That(text).IsEqualTo("sample");
        await Assert.That(tags).IsNotNull();
        await Assert.That(tags.Length).IsEqualTo(0);
    }

    [Test]
    [Arguments("sample", "")]
    public async Task ParamsWithEmptyString(string text, params string[] tags)
    {
        await Assert.That(text).IsEqualTo("sample");
        await Assert.That(tags).IsNotNull();
        await Assert.That(tags.Length).IsEqualTo(1);
        await Assert.That(tags[0]).IsEqualTo("");
    }

    [Test]
    [Arguments("sample", "", "other")]
    public async Task ParamsWithEmptyStringAndOthers(string text, params string[] tags)
    {
        await Assert.That(text).IsEqualTo("sample");
        await Assert.That(tags).IsNotNull();
        await Assert.That(tags.Length).IsEqualTo(2);
        await Assert.That(tags[0]).IsEqualTo("");
        await Assert.That(tags[1]).IsEqualTo("other");
    }
}
