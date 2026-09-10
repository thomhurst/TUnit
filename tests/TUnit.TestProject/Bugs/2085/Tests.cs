using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._2085;

[EngineTest(ExpectedResult.Pass)]
public class Tests
{
    [Test]
    [MatrixDataSource]
    public async Task Double_SpecialConsts([Matrix(double.NaN, double.PositiveInfinity, double.NegativeInfinity)] double d)
    {
        await Assert.That(d).IsNotEqualTo(0);
    }

    [Test]
    [MatrixDataSource]
    public async Task Float_SpecialConsts([Matrix(float.NaN, float.PositiveInfinity, float.NegativeInfinity)] float d)
    {
        await Assert.That(d).IsNotEqualTo(0);
    }
}
