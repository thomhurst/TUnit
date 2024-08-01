using System.Collections.Immutable;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

public class ParametersTests
{
    [Test]
    public async Task Test()
    {
        await using (Assert.Multiple())
        {
            Assert.That(TestContext.Parameters.ToImmutableDictionary()).Does.ContainKey("TestParam1");
            Assert.That(TestContext.Parameters["TestParam1"]).Is.EqualTo("TestParamValue1");
        }
    }
}