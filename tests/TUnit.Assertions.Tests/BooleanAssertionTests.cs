using TUnit.Assertions.Extensions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

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

    [Test]
    public async Task Test_NullableBoolean_IsTrue_WithTrue()
    {
        bool? value = true;
        await Assert.That(value).IsTrue();
    }

    [Test]
    public async Task Test_NullableBoolean_IsTrue_WithFalse()
    {
        bool? value = false;
        await Assert.That(async () => await Assert.That(value).IsTrue())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Test_NullableBoolean_IsTrue_WithNull()
    {
        bool? value = null;
        await Assert.That(async () => await Assert.That(value).IsTrue())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Test_NullableBoolean_IsFalse_WithFalse()
    {
        bool? value = false;
        await Assert.That(value).IsFalse();
    }

    [Test]
    public async Task Test_NullableBoolean_IsFalse_WithTrue()
    {
        bool? value = true;
        await Assert.That(async () => await Assert.That(value).IsFalse())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Test_NullableBoolean_IsFalse_WithNull()
    {
        bool? value = null;
        await Assert.That(async () => await Assert.That(value).IsFalse())
            .Throws<AssertionException>();
    }
}
