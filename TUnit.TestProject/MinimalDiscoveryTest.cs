using TUnit.Core;

namespace TUnit.TestProject;

public class MinimalDiscoveryTest
{
    [Test]
    public async Task SimpleTest()
    {
        await Assert.That(1 + 1).IsEqualTo(2);
    }
}