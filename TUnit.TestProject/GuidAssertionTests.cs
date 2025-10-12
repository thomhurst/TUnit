using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class GuidAssertionTests
{
    [Test]
    public async Task Test_Guid_IsEmptyGuid()
    {
        var guid = Guid.Empty;
        await Assert.That(guid).IsEmptyGuid();
    }

    [Test]
    public async Task Test_Guid_IsNotEmptyGuid()
    {
        var guid = Guid.NewGuid();
        await Assert.That(guid).IsNotEmptyGuid();
    }

    [Test]
    public async Task Test_Guid_IsNotEmptyGuid_WithSpecificGuid()
    {
        var guid = new Guid("12345678-1234-1234-1234-123456789012");
        await Assert.That(guid).IsNotEmptyGuid();
    }
}
