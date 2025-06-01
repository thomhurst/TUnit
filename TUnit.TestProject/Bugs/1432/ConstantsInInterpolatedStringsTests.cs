using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._1432;

[EngineTest(ExpectedResult.Pass)]
internal abstract class ConstantsInInterpolatedStringsTestsBase
{
    protected const string BaseValue = "Value";
}

internal class ConstantsInInterpolatedStringsTests : ConstantsInInterpolatedStringsTestsBase
{
    [Test]
    [Arguments($"{BaseValue}1")]
    public async Task SomeTest(string value)
    {
        await Assert.That(value).IsEqualTo(value);
    }
}