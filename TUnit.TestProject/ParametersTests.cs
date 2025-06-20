using System.Collections.Immutable;

namespace TUnit.TestProject;

public class ParametersTests
{
    [Test]
    public async Task Test()
    {
        using (Assert.Multiple())
        {
            await Assert.That(TestContext.Parameters.ToImmutableDictionary()).ContainsKey("TestParam1");
            await Assert.That(TestContext.Parameters["TestParam1"]).IsEqualTo("TestParam1Value");
        }
    }
}