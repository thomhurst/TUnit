using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject.Bugs._1432;

internal abstract class ConstantInBaseClassTestsBase
{
    protected const string BaseValue = "Value";
}

internal class ConstantInBaseClassTests : ConstantInBaseClassTestsBase
{
    [Test]
    [Arguments(BaseValue)]
    public async Task SomeTest(string value)
    {
        await Assert.That(value).IsEqualTo(value);
    }
}