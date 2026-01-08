using TUnit.Core;

namespace TUnit.TestProject;

public class JUnitReporterInvalidXmlCharacterTests
{
    [Test]
    [Arguments("Some valid string")]
    public async Task TestWithValidString(string parameter)
    {
        await Assert.That(parameter).IsNotNull();
    }

    [Test]
    [Arguments("A string with an invalid \x04 character")]
    public async Task TestWithInvalidXmlCharacter(string parameter)
    {
        await Assert.That(parameter).IsNotNull();
    }

    [Test]
    [Arguments("Multiple\x01\x02\x03\x04invalid characters")]
    public async Task TestWithMultipleInvalidXmlCharacters(string parameter)
    {
        await Assert.That(parameter).IsNotNull();
    }

    [Test]
    [Arguments("Test\twith\nvalid\rcontrol characters")]
    public async Task TestWithValidControlCharacters(string parameter)
    {
        await Assert.That(parameter).IsNotNull();
    }
}
