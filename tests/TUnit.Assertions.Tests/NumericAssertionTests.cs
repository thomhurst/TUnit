using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class NumericAssertionTests
{
    // Long tests
    [Test]
    public async Task Test_Long_IsZero()
    {
        long value = 0;
        await Assert.That(value).IsZero();
    }

    [Test]
    public async Task Test_Long_IsZero_Fails_WhenNotZero()
    {
        long value = 1;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsZero());
    }

    [Test]
    public async Task Test_Long_IsNotZero()
    {
        long value = 1234567890L;
        await Assert.That(value).IsNotZero();
    }

    [Test]
    public async Task Test_Long_IsNotZero_Fails_WhenZero()
    {
        long value = 0;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsNotZero());
    }

    [Test]
    public async Task Test_Long_IsEven()
    {
        long value = 2;
        await Assert.That(value).IsEven();
    }

    [Test]
    public async Task Test_Long_IsEven_Fails_WhenOdd()
    {
        long value = 3;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsEven());
    }

    [Test]
    public async Task Test_Long_IsOdd()
    {
        long value = 3;
        await Assert.That(value).IsOdd();
    }

    [Test]
    public async Task Test_Long_IsOdd_Fails_WhenEven()
    {
        long value = 4;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsOdd());
    }

    // Double tests
    [Test]
    public async Task Test_Double_IsZero()
    {
        double value = 0.0;
        await Assert.That(value).IsZero();
    }

    [Test]
    public async Task Test_Double_IsZero_Fails_WhenNotZero()
    {
        double value = 0.1;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsZero());
    }

    [Test]
    public async Task Test_Double_IsNotZero()
    {
        double value = 1.5;
        await Assert.That(value).IsNotZero();
    }

    [Test]
    public async Task Test_Double_IsNotZero_Negative()
    {
        double value = -3.14;
        await Assert.That(value).IsNotZero();
    }

    [Test]
    public async Task Test_Double_IsNotZero_Fails_WhenZero()
    {
        double value = 0.0;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsNotZero());
    }

    // Float tests
    [Test]
    public async Task Test_Float_IsZero()
    {
        float value = 0.0f;
        await Assert.That(value).IsZero();
    }

    [Test]
    public async Task Test_Float_IsZero_Fails_WhenNotZero()
    {
        float value = 0.1f;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsZero());
    }

    [Test]
    public async Task Test_Float_IsNotZero()
    {
        float value = 2.5f;
        await Assert.That(value).IsNotZero();
    }

    [Test]
    public async Task Test_Float_IsNotZero_Fails_WhenZero()
    {
        float value = 0.0f;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsNotZero());
    }

    // Decimal tests
    [Test]
    public async Task Test_Decimal_IsZero()
    {
        decimal value = 0m;
        await Assert.That(value).IsZero();
    }

    [Test]
    public async Task Test_Decimal_IsZero_Fails_WhenNotZero()
    {
        decimal value = 0.01m;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsZero());
    }

    [Test]
    public async Task Test_Decimal_IsNotZero()
    {
        decimal value = 99.99m;
        await Assert.That(value).IsNotZero();
    }

    [Test]
    public async Task Test_Decimal_IsNotZero_Fails_WhenZero()
    {
        decimal value = 0m;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsNotZero());
    }

    // Short tests
    [Test]
    public async Task Test_Short_IsZero()
    {
        short value = 0;
        await Assert.That(value).IsZero();
    }

    [Test]
    public async Task Test_Short_IsZero_Fails_WhenNotZero()
    {
        short value = 1;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsZero());
    }

    [Test]
    public async Task Test_Short_IsNotZero()
    {
        short value = 100;
        await Assert.That(value).IsNotZero();
    }

    [Test]
    public async Task Test_Short_IsNotZero_Fails_WhenZero()
    {
        short value = 0;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsNotZero());
    }

    [Test]
    public async Task Test_Short_IsEven()
    {
        short value = 4;
        await Assert.That(value).IsEven();
    }

    [Test]
    public async Task Test_Short_IsEven_Fails_WhenOdd()
    {
        short value = 5;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsEven());
    }

    [Test]
    public async Task Test_Short_IsOdd()
    {
        short value = 5;
        await Assert.That(value).IsOdd();
    }

    [Test]
    public async Task Test_Short_IsOdd_Fails_WhenEven()
    {
        short value = 6;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsOdd());
    }

    // Byte tests
    [Test]
    public async Task Test_Byte_IsZero()
    {
        byte value = 0;
        await Assert.That(value).IsZero();
    }

    [Test]
    public async Task Test_Byte_IsZero_Fails_WhenNotZero()
    {
        byte value = 1;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsZero());
    }

    [Test]
    public async Task Test_Byte_IsNotZero()
    {
        byte value = 255;
        await Assert.That(value).IsNotZero();
    }

    [Test]
    public async Task Test_Byte_IsNotZero_Fails_WhenZero()
    {
        byte value = 0;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsNotZero());
    }

    [Test]
    public async Task Test_Byte_IsEven()
    {
        byte value = 10;
        await Assert.That(value).IsEven();
    }

    [Test]
    public async Task Test_Byte_IsEven_Fails_WhenOdd()
    {
        byte value = 11;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsEven());
    }

    [Test]
    public async Task Test_Byte_IsOdd()
    {
        byte value = 11;
        await Assert.That(value).IsOdd();
    }

    [Test]
    public async Task Test_Byte_IsOdd_Fails_WhenEven()
    {
        byte value = 12;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsOdd());
    }

    // Uint tests
    [Test]
    public async Task Test_Uint_IsZero()
    {
        uint value = 0;
        await Assert.That(value).IsZero();
    }

    [Test]
    public async Task Test_Uint_IsZero_Fails_WhenNotZero()
    {
        uint value = 1;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsZero());
    }

    [Test]
    public async Task Test_Uint_IsNotZero()
    {
        uint value = 12345;
        await Assert.That(value).IsNotZero();
    }

    [Test]
    public async Task Test_Uint_IsNotZero_Fails_WhenZero()
    {
        uint value = 0;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsNotZero());
    }

    [Test]
    public async Task Test_Uint_IsEven()
    {
        uint value = 6;
        await Assert.That(value).IsEven();
    }

    [Test]
    public async Task Test_Uint_IsEven_Fails_WhenOdd()
    {
        uint value = 7;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsEven());
    }

    [Test]
    public async Task Test_Uint_IsOdd()
    {
        uint value = 7;
        await Assert.That(value).IsOdd();
    }

    [Test]
    public async Task Test_Uint_IsOdd_Fails_WhenEven()
    {
        uint value = 8;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsOdd());
    }

    // Ulong tests
    [Test]
    public async Task Test_Ulong_IsZero()
    {
        ulong value = 0;
        await Assert.That(value).IsZero();
    }

    [Test]
    public async Task Test_Ulong_IsZero_Fails_WhenNotZero()
    {
        ulong value = 1;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsZero());
    }

    [Test]
    public async Task Test_Ulong_IsNotZero()
    {
        ulong value = 9876543210UL;
        await Assert.That(value).IsNotZero();
    }

    [Test]
    public async Task Test_Ulong_IsNotZero_Fails_WhenZero()
    {
        ulong value = 0;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsNotZero());
    }

    [Test]
    public async Task Test_Ulong_IsEven()
    {
        ulong value = 8;
        await Assert.That(value).IsEven();
    }

    [Test]
    public async Task Test_Ulong_IsEven_Fails_WhenOdd()
    {
        ulong value = 9;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsEven());
    }

    [Test]
    public async Task Test_Ulong_IsOdd()
    {
        ulong value = 9;
        await Assert.That(value).IsOdd();
    }

    [Test]
    public async Task Test_Ulong_IsOdd_Fails_WhenEven()
    {
        ulong value = 10;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsOdd());
    }

    // Ushort tests
    [Test]
    public async Task Test_Ushort_IsZero()
    {
        ushort value = 0;
        await Assert.That(value).IsZero();
    }

    [Test]
    public async Task Test_Ushort_IsZero_Fails_WhenNotZero()
    {
        ushort value = 1;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsZero());
    }

    [Test]
    public async Task Test_Ushort_IsNotZero()
    {
        ushort value = 500;
        await Assert.That(value).IsNotZero();
    }

    [Test]
    public async Task Test_Ushort_IsNotZero_Fails_WhenZero()
    {
        ushort value = 0;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsNotZero());
    }

    [Test]
    public async Task Test_Ushort_IsEven()
    {
        ushort value = 12;
        await Assert.That(value).IsEven();
    }

    [Test]
    public async Task Test_Ushort_IsEven_Fails_WhenOdd()
    {
        ushort value = 13;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsEven());
    }

    [Test]
    public async Task Test_Ushort_IsOdd()
    {
        ushort value = 13;
        await Assert.That(value).IsOdd();
    }

    [Test]
    public async Task Test_Ushort_IsOdd_Fails_WhenEven()
    {
        ushort value = 14;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsOdd());
    }

    // Sbyte tests
    [Test]
    public async Task Test_Sbyte_IsZero()
    {
        sbyte value = 0;
        await Assert.That(value).IsZero();
    }

    [Test]
    public async Task Test_Sbyte_IsZero_Fails_WhenNotZero()
    {
        sbyte value = 1;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsZero());
    }

    [Test]
    public async Task Test_Sbyte_IsNotZero()
    {
        sbyte value = -50;
        await Assert.That(value).IsNotZero();
    }

    [Test]
    public async Task Test_Sbyte_IsNotZero_Fails_WhenZero()
    {
        sbyte value = 0;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsNotZero());
    }

    [Test]
    public async Task Test_Sbyte_IsEven()
    {
        sbyte value = 14;
        await Assert.That(value).IsEven();
    }

    [Test]
    public async Task Test_Sbyte_IsEven_Fails_WhenOdd()
    {
        sbyte value = 15;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsEven());
    }

    [Test]
    public async Task Test_Sbyte_IsOdd()
    {
        sbyte value = 15;
        await Assert.That(value).IsOdd();
    }

    [Test]
    public async Task Test_Sbyte_IsOdd_Fails_WhenEven()
    {
        sbyte value = 16;
        await Assert.ThrowsAsync<AssertionException>(async () => await Assert.That(value).IsOdd());
    }
}
