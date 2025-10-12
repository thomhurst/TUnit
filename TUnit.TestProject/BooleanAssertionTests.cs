using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class BooleanAssertionTests
{
    [Test]
    public async Task Test_Boolean_IsTrue()
    {
        var value = true;
        await Assert.That(value).IsTrue();
    }

    [Test]
    public async Task Test_Boolean_IsTrue_FromExpression()
    {
        var result = 5 > 3;
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Test_Boolean_IsFalse()
    {
        var value = false;
        await Assert.That(value).IsFalse();
    }

    [Test]
    public async Task Test_Boolean_IsFalse_FromExpression()
    {
        var result = 5 < 3;
        await Assert.That(result).IsFalse();
    }
}
