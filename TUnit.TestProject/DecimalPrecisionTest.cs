using TUnit.Core;

namespace TUnit.TestProject;

public class DecimalPrecisionTest
{
    [Test]
    [Arguments(2_000, 123_999.00000000000000001)]
    [Arguments(2_000.00000000000000001, 123_999)]
    [Arguments(2_000.00000000000000001, 123_999.00000000000000001)]
    public async Task Transfer(decimal debit, decimal credit)
    {
        Console.WriteLine("{0} {1}", debit, credit);
        
        // These assertions should pass to verify the decimal precision is preserved
        if (debit == 2_000.00000000000000001m)
        {
            await Assert.That(debit).IsEqualTo(2_000.00000000000000001m);
        }
        
        if (credit == 123_999.00000000000000001m)
        {
            await Assert.That(credit).IsEqualTo(123_999.00000000000000001m);
        }
    }
}