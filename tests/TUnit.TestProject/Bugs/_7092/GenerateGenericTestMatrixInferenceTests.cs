using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7092;

[EngineTest(ExpectedResult.Pass)]
public class GenerateGenericTestMatrixInferenceTests
{
    // Matrix<int> implements IInfersType<int>, so source-gen mode adds an <int> instantiation next to the
    // [GenerateGenericTest] one. Both instantiations run every matrix row.
    [Test]
    [MatrixDataSource]
    [GenerateGenericTest(typeof(long))]
    public async Task Matrix_Infers_Its_Own_Type<T>([Matrix<int>(1, 2)] T value)
    {
        await Assert.That(value is T).IsTrue();
    }
}
