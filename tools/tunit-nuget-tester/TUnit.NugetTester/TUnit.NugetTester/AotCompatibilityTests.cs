using TUnit.NugetTester.Library;

namespace TUnit.NugetTester;

/// <summary>
/// Tests for AOT compatibility - verifies issue #3321 is fixed.
/// These tests should compile without IL3050 warnings when PublishAot=true.
/// </summary>
public class AotCompatibilityTests : TestBase
{
    public enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }

    [Test]
    [Arguments(TestEnum.Value1)]
    [Arguments(TestEnum.Value2)]
    [Arguments(TestEnum.Value3)]
    public void EnumArgumentTest(TestEnum value)
    {
        // This test verifies that enum arguments work without AOT warnings
        Assert.That(value).IsIn(TestEnum.Value1, TestEnum.Value2, TestEnum.Value3);
    }

    [Test]
    [Arguments(new int[] { 1, 2, 3 })]
    [Arguments(new int[] { 4, 5, 6 })]
    public void ArrayArgumentTest(int[] values)
    {
        // This test verifies that array arguments work without AOT warnings
        Assert.That(values).IsNotNull();
        Assert.That(values).HasCount(3);
    }

    [Test]
    [Arguments(42)]
    [Arguments(100)]
    public void IntArgumentTest(int value)
    {
        // This test verifies that primitive type arguments work without AOT warnings
        Assert.That(value).IsGreaterThan(0);
    }

    [Test]
    [Arguments("test")]
    [Arguments("hello")]
    public void StringArgumentTest(string value)
    {
        // This test verifies that string arguments work without AOT warnings
        Assert.That(value).IsNotEmpty();
    }

    [Test]
    [Arguments(3.14)]
    [Arguments(2.71)]
    public void DoubleArgumentTest(double value)
    {
        // This test verifies that double arguments work without AOT warnings
        Assert.That(value).IsGreaterThan(0.0);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public void BoolArgumentTest(bool value)
    {
        // This test verifies that bool arguments work without AOT warnings
        Assert.That(value).IsIn(true, false);
    }
}
