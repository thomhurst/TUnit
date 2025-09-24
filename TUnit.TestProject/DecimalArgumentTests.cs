using System.Globalization;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class DecimalArgumentTests
{
    [Test]
    [Arguments(2_000, 123_999.00000000000000001)]
    [Arguments(2_000.00000000000000001, 123_999)]
    [Arguments(2_000.00000000000000001, 123_999.00000000000000001)]
    public async Task Transfer(decimal debit, decimal credit)
    {
        // Test that the decimal values maintain their precision
        // Check if the value is one of the expected values (with or without extended precision)
        await Assert.That(debit is 2_000m or 2_000.00000000000000001m).IsTrue();
        await Assert.That(credit is 123_999m or 123_999.00000000000000001m).IsTrue();

        // The precision test - these should preserve the original literal values
        if (debit == 2_000.00000000000000001m && credit == 123_999.00000000000000001m)
        {
            // This test case has both values with extended precision
            await Assert.That(debit.ToString(CultureInfo.InvariantCulture)).IsEqualTo("2000.00000000000000001");
            await Assert.That(credit.ToString(CultureInfo.InvariantCulture)).IsEqualTo("123999.00000000000000001");
        }
    }

    [Test]
    [Arguments(123.456)]
    public async Task SimpleDecimal(decimal value)
    {
        await Assert.That(value).IsEqualTo(123.456m);
    }

    [Test]
    [Arguments(0.00000000000000001)]
    public async Task SmallDecimal(decimal value)
    {
        await Assert.That(value).IsEqualTo(0.00000000000000001m);
    }

    [Test]
    [Arguments("79228162514264337593543950335")] // Max decimal value as string
    public async Task MaxDecimal(decimal value)
    {
        await Assert.That(value).IsEqualTo(decimal.MaxValue);
    }

    [Test]
    [Arguments("-79228162514264337593543950335")] // Min decimal value as string
    public async Task MinDecimal(decimal value)
    {
        await Assert.That(value).IsEqualTo(decimal.MinValue);
    }

    [Test]
    [Arguments("123.456")] // Decimal value as string
    public async Task ExplicitDecimalValue(decimal value)
    {
        await Assert.That(value).IsEqualTo(123.456m);
    }

    [Test]
    [Arguments(1.1, 2.2, 3.3)] // Multiple decimal arguments
    public async Task MultipleDecimals(decimal a, decimal b, decimal c)
    {
        await Assert.That(a).IsEqualTo(1.1m);
        await Assert.That(b).IsEqualTo(2.2m);
        await Assert.That(c).IsEqualTo(3.3m);
    }

    [Test]
    [Arguments(0.5)]
    [Arguments(0.75)]
    [Arguments(1)]
    public void Test(decimal test)
    {
    }


    [Test]
    [Arguments(50, 75, 70, 5, 0, true)]
    [Arguments(70, 75, 70, 5, 5, true)]
    [Arguments(70, 75, 70, 5, 0, false)]
    public void TransactionDiscountCalculations(decimal amountPaying, decimal invoiceBalance,
        decimal invoiceBalanceDue, decimal discountAmount, decimal appliedDiscountAmount, bool discountAllowedForUser)
    {
    }
}
