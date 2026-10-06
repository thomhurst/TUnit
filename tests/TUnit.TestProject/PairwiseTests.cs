using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class PairwiseTests
{
    // 2 x 2 x 4 x 4 = 64 combinations; pairwise needs 16.
    [Test]
    [PairwiseDataSource]
    public async Task Pairwise_Bools_And_Ints(
        bool largeArrayPool,
        bool excessSpan,
        [Matrix(0, 99, 100, 101)] int sizeHint,
        [Matrix(1, 2, 3, 100)] int stepCount)
    {
        await Task.CompletedTask;
    }

    // 3 x 3 x 3 x 3 = 81 combinations; pairwise needs 9.
    [Test]
    [PairwiseDataSource]
    public async Task Pairwise_Strings_And_Ints(
        [Matrix("A", "B", "C")] string a,
        [Matrix("D", "E", "F")] string b,
        [Matrix(1, 2, 3)] int c,
        [Matrix(4, 5, 6)] int d)
    {
        await Task.CompletedTask;
    }

    // enum (3) x bool? (3) x 5 x 2 x 3 = 270 combinations; pairwise needs 17.
    [Test]
    [PairwiseDataSource]
    public async Task Pairwise_Enum_And_Nullable(
        PairwiseColor color,
        bool? flag,
        [Matrix(1, 2, 3, 4, 5)] int number,
        [Matrix("x", "y")] string text,
        [Matrix(1, 2, 3)] int other)
    {
        await Assert.That(Enum.IsDefined(typeof(PairwiseColor), color)).IsTrue();
    }

    // Nullable enum gets its values plus null, like [MatrixDataSource]: 4 x 2 x 2 = 16 combinations.
    [Test]
    [PairwiseDataSource]
    public async Task Pairwise_Nullable_Enum(
        PairwiseColor? color,
        bool flag,
        [Matrix("x", "y")] string text)
    {
        await Task.CompletedTask;
    }

    [Test]
    [PairwiseDataSource]
    public async Task Pairwise_Matrix_Excluding(
        [Matrix(1, 2, 3, 4, Excluding = [2])] int number,
        [Matrix<PairwiseColor>(Excluding = [PairwiseColor.Green])] PairwiseColor color,
        bool flag)
    {
        await Assert.That(number).IsNotEqualTo(2);
        await Assert.That(color).IsNotEqualTo(PairwiseColor.Green);
    }

    [Test]
    [PairwiseDataSource]
    [MatrixExclusion(1, 1, 1)]
    [MatrixExclusion(2, 2, 2)]
    [MatrixExclusion(3, 3, 3)]
    public async Task Pairwise_Exclusion(
        [MatrixMethod<PairwiseTests>(nameof(Values))] int a,
        [MatrixMethod<PairwiseTests>(nameof(Values))] int b,
        [MatrixMethod<PairwiseTests>(nameof(Values))] int c)
    {
        await Assert.That(a == b && b == c).IsFalse();
    }

    [Test]
    [PairwiseDataSource]
    [MatrixExclusion(PairwiseColor.Red, true, 1)]
    [MatrixExclusion(PairwiseColor.Blue, false, 3)]
    public async Task Pairwise_Enum_Exclusion(
        PairwiseColor color,
        bool flag,
        [Matrix(1, 2, 3)] int number)
    {
        await Assert.That(color == PairwiseColor.Red && flag && number == 1).IsFalse();
        await Assert.That(color == PairwiseColor.Blue && !flag && number == 3).IsFalse();
    }

    [Test]
    [PairwiseDataSource(Seed = 42)]
    public async Task Pairwise_Custom_Seed(
        [Matrix(1, 2, 3)] int a,
        [Matrix(1, 2, 3, 4)] int b,
        [Matrix(1, 2, 3, 4, 5)] int c,
        [Matrix(1, 2, 3, 4, 5, 6)] int d)
    {
        await Task.CompletedTask;
    }

#if NET8_0_OR_GREATER
    // 10 x 10 x 10 = 1000 combinations; pairwise needs 106.
    [Test]
    [PairwiseDataSource]
    public async Task Pairwise_Range(
        [MatrixRange<int>(1, 10)] int a,
        [MatrixRange<int>(1, 10)] int b,
        [MatrixRange<int>(1, 10)] int c)
    {
        await Task.CompletedTask;
    }
#endif

    public static IEnumerable<int> Values()
    {
        yield return 1;
        yield return 2;
        yield return 3;
    }

    public enum PairwiseColor
    {
        Red,
        Green,
        Blue
    }
}
