namespace TUnit.Assertions.Tests;

public class NumericEqualityToleranceAssertionTests
{
    [Test]
    public async Task Double_RelativeTolerance_Passes_When_Within_Percentage()
    {
        const double actual = 104.9;
        const double expected = 100.0;

        await Assert.That(actual)
            .IsEqualTo(expected)
            .WithinRelativeTolerance(5);
    }

    [Test]
    public async Task Double_RelativeTolerance_Fails_When_Outside_Percentage()
    {
        const double actual = 105.1;
        const double expected = 100.0;

        var sut = async () => await Assert.That(actual)
            .IsEqualTo(expected)
            .WithinRelativeTolerance(5);

        await Assert.That(sut).ThrowsException();
    }

    [Test]
    public async Task Double_RelativeTolerance_Includes_Exact_Boundary()
    {
        const double actual = 105.0;
        const double expected = 100.0;

        await Assert.That(actual)
            .IsEqualTo(expected)
            .WithinRelativeTolerance(5);
    }

    [Test]
    public async Task Double_RelativeTolerance_Works_With_Negative_Expected_Value()
    {
        const double actual = -104.9;
        const double expected = -100.0;

        await Assert.That(actual)
            .IsEqualTo(expected)
            .WithinRelativeTolerance(5);
    }

    [Test]
    public async Task Double_RelativeTolerance_Fails_With_Negative_Expected_Value_When_Outside_Boundary()
    {
        const double actual = -106.0;
        const double expected = -100.0;

        var sut = async () => await Assert.That(actual)
            .IsEqualTo(expected)
            .WithinRelativeTolerance(5);

        await Assert.That(sut).ThrowsException();
    }

    [Test]
    public async Task Double_RelativeTolerance_ZeroExpected_Passes_Only_For_Exact_Zero()
    {
        const double actual = 0.0;
        const double expected = 0.0;

        await Assert.That(actual)
            .IsEqualTo(expected)
            .WithinRelativeTolerance(5);
    }

    [Test]
    public async Task Double_RelativeTolerance_ZeroExpected_Fails_For_NonZero_Actual()
    {
        const double actual = 0.0001;
        const double expected = 0.0;

        var sut = async () => await Assert.That(actual)
            .IsEqualTo(expected)
            .WithinRelativeTolerance(5);

        await Assert.That(sut).ThrowsException();
    }

    [Test]
    public async Task Double_RelativeTolerance_Treats_NaN_As_Equal_To_NaN()
    {
        const double actual = double.NaN;
        const double expected = double.NaN;

        await Assert.That(actual)
            .IsEqualTo(expected)
            .WithinRelativeTolerance(5);
    }

    [Test]
    public async Task Double_RelativeTolerance_Fails_When_Only_One_Side_Is_NaN()
    {
        const double actual = double.NaN;
        const double expected = 100.0;

        var sut = async () => await Assert.That(actual)
            .IsEqualTo(expected)
            .WithinRelativeTolerance(5);

        await Assert.That(sut).ThrowsException();
    }

    [Test]
    public async Task Double_RelativeTolerance_Treats_Matching_Positive_Infinity_As_Equal()
    {
        const double actual = double.PositiveInfinity;
        const double expected = double.PositiveInfinity;

        await Assert.That(actual)
            .IsEqualTo(expected)
            .WithinRelativeTolerance(5);
    }

    [Test]
    public async Task Double_RelativeTolerance_Fails_For_Mismatched_Infinities()
    {
        const double actual = double.PositiveInfinity;
        const double expected = double.NegativeInfinity;

        var sut = async () => await Assert.That(actual)
            .IsEqualTo(expected)
            .WithinRelativeTolerance(5);

        await Assert.That(sut).ThrowsException();
    }

    [Test]
    public async Task Float_RelativeTolerance_Passes_When_Within_Percentage()
    {
        const float actual = 104.9f;
        const float expected = 100f;

        await Assert.That(actual)
            .IsEqualTo(expected)
            .WithinRelativeTolerance(5);
    }

    [Test]
    public async Task Float_RelativeTolerance_Treats_NaN_As_Equal_To_NaN()
    {
        const float actual = float.NaN;
        const float expected = float.NaN;

        await Assert.That(actual)
            .IsEqualTo(expected)
            .WithinRelativeTolerance(5);
    }

    [Test]
    public async Task Float_RelativeTolerance_Treats_Matching_Infinity_As_Equal()
    {
        const float actual = float.PositiveInfinity;
        const float expected = float.PositiveInfinity;

        await Assert.That(actual)
            .IsEqualTo(expected)
            .WithinRelativeTolerance(5);
    }

    [Test]
    public async Task Decimal_RelativeTolerance_Passes_When_Within_Percentage()
    {
        const decimal actual = 104.9m;
        const decimal expected = 100m;

        await Assert.That(actual)
            .IsEqualTo(expected)
            .WithinRelativeTolerance(5);
    }

    [Test]
    public async Task Decimal_RelativeTolerance_Fails_When_Outside_Percentage()
    {
        const decimal actual = 105.1m;
        const decimal expected = 100m;

        var sut = async () => await Assert.That(actual)
            .IsEqualTo(expected)
            .WithinRelativeTolerance(5);

        await Assert.That(sut).ThrowsException();
    }

    [Test]
    public async Task RelativeTolerance_Exception_Message_Contains_Expectation_Text()
    {
        const double actual = 106.0;
        const double expected = 100.0;

        var sut = async () => await Assert.That(actual)
            .IsEqualTo(expected)
            .WithinRelativeTolerance(5);

        var exception = await Assert.That(sut).ThrowsException();

        await Assert.That(exception.Message.NormalizeLineEndings())
            .Contains("Expected to be within 5% of 100")
            .And.Contains("but found 106")
            .And.Contains("Assert.That(actual).IsEqualTo(expected).WithinRelativeTolerance(5)");
    }

    [Test]
    public async Task RelativeTolerance_Exception_Message_Contains_Difference_Details()
    {
        const double actual = 106.0;
        const double expected = 100.0;

        var sut = async () => await Assert.That(actual)
            .IsEqualTo(expected)
            .WithinRelativeTolerance(5);

        var exception = await Assert.That(sut).ThrowsException();

        await Assert.That(exception.Message.NormalizeLineEndings())
            .Contains("differs by 6")
            .And.Contains("relative tolerance of 5%");
    }
}
