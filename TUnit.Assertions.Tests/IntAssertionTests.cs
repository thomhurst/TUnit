using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class IntAssertionTests
{
    [Test]
    public async Task Test_Int_IsZero()
    {
        var value = 0;
        await Assert.That(value).IsZero();
    }

    [Test]
    public async Task Test_Int_IsZero_Literal()
    {
        await Assert.That(0).IsZero();
    }

    [Test]
    public async Task Test_Int_IsNotZero_Positive()
    {
        var value = 1;
        await Assert.That(value).IsNotZero();
    }

    [Test]
    public async Task Test_Int_IsNotZero_Negative()
    {
        var value = -1;
        await Assert.That(value).IsNotZero();
    }

    [Test]
    public async Task Test_Int_IsNotZero_Large()
    {
        var value = 1000000;
        await Assert.That(value).IsNotZero();
    }

    [Test]
    public async Task Test_Int_IsEven_Zero()
    {
        var value = 0;
        await Assert.That(value).IsEven();
    }

    [Test]
    public async Task Test_Int_IsEven_Positive()
    {
        var value = 2;
        await Assert.That(value).IsEven();
    }

    [Test]
    public async Task Test_Int_IsEven_Negative()
    {
        var value = -4;
        await Assert.That(value).IsEven();
    }

    [Test]
    public async Task Test_Int_IsOdd_Positive()
    {
        var value = 3;
        await Assert.That(value).IsOdd();
    }

    [Test]
    public async Task Test_Int_IsOdd_Negative()
    {
        var value = -5;
        await Assert.That(value).IsOdd();
    }
}
