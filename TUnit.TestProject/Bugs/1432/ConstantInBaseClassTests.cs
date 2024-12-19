using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject.Bugs._1432;

internal abstract class BaseTest
{
    protected const string BaseValue = "Value";
}

internal class ConstantInBaseClassTests : BaseTest
{
    [Test]
    [Arguments(BaseValue)]
    public async Task SomeTest(string value)
    {
        await Assert.That(value).IsEqualTo(value);
    }
}